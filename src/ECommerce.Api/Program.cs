using ECommerce.Api.Extensions;
using ECommerce.Api.Filters;
using ECommerce.Api.Hubs;
using ECommerce.Api.Middleware;
using ECommerce.Api.Services;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Consumers;
using ECommerce.Application.Wishlist.Interfaces;
using ECommerce.Application.Wishlist.Services;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Seeding;
using ECommerce.Infrastructure.Services;
using FluentValidation;
using Hangfire;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
}

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errorDict = new Dictionary<string, List<string>>();
        foreach (var error in context.ModelState.Where(e => e.Value?.Errors.Count > 0))
        {
            errorDict[error.Key] = [.. error.Value!.Errors.Select(e => e.ErrorMessage)];
        }
        
        return new BadRequestObjectResult(new
        {
            success = false,
            message = "Validation failed",
            errors = errorDict
        });
    };
});
builder.Services.AddValidatorsFromAssemblyContaining<Program>(lifetime: ServiceLifetime.Scoped);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ECommerce API",
        Version = "v1",
        Description = "A comprehensive e-commerce REST API with product management, category management, and JWT authentication",
        Contact = new OpenApiContact
        {
            Name = "ECommerce Team",
            Email = "support@ecommerce.com"
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIs...",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    c.MapType<Guid>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "uuid",
        Example = new Microsoft.OpenApi.Any.OpenApiString("00000000-0000-0000-0000-000000000000")
    });
});

builder.Services.AddDatabaseContext(builder.Configuration);

builder.Services.AddApplicationServices(builder.Configuration);

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHangfire(config => config.UseInMemoryStorage());
}
else
{
    builder.Services.AddHangfire(config =>
        config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
}

builder.Services.AddHangfireServer();

builder.Services.AddRepositories();

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var rsaPublicKey = LoadRsaPublicKey(builder.Environment.IsEnvironment("Testing"));
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var path = context.HttpContext.Request.Path;

            var accessToken = context.Request.Cookies["access_token"];

            if (string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                accessToken = context.Request.Query["access_token"];
            }

            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = rsaPublicKey,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var rabbitHost = builder.Configuration["RabbitMq:Host"] ?? "localhost";

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderStatusChangedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddSignalR();

builder.Services.AddScoped<INotificationService, SignalRNotificationService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(
        name: "database",
        tags: HealthCheckTags.Ready);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("global", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("products", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("categories", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("cart", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("ECommerce.Api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation()
        .AddSource("MassTransit")
        .AddOtlpExporter(options => 
   {
       var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
       var seqHost = isDocker ? "seq" : "localhost";
       
       options.Endpoint = new Uri($"http://{seqHost}:5341/ingest/otlp/v1/traces");
       options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
   })
    );

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<ICartCleanupService>(
        "cart-cleanup",
        service => service.CleanupAbandonedCartsAsync(default),
        Cron.Daily);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ECommerce API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "ECommerce API Documentation";
        c.DisplayRequestDuration();
        c.DefaultModelsExpandDepth(-1);
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseStaticFiles();

app.UseCors("AllowAngular");

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("Referrer-Policy", "no-referrer");
    await next();
});

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAuthorizationFilter()]
});

app.MapControllers();

app.MapHealthChecks("/health");

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHub<NotificationHub>("/hubs/notifications");

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        await context.Database.CanConnectAsync();
        await DatabaseSeeder.SeedAsync(context);
        logger.LogInformation("Database seeded successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database seeding failed: {Message}", ex.Message);

        if (!app.Environment.IsDevelopment())
        {
            throw;
        }

        logger.LogWarning("Running without seeded data. The API will start but database-dependent endpoints may fail.");
    }
}

app.Run();

static RsaSecurityKey LoadRsaPublicKey(bool useEphemeralTestingKey)
{
    var publicKeyBase64 = Environment.GetEnvironmentVariable("JWT_PUBLIC_KEY_BASE64");
    var publicKeyPath = Environment.GetEnvironmentVariable("JWT_PUBLIC_KEY_PATH");
    string pem;

    if (!string.IsNullOrEmpty(publicKeyBase64))
    {
        pem = Encoding.UTF8.GetString(Convert.FromBase64String(publicKeyBase64));
    }
    else if (!string.IsNullOrEmpty(publicKeyPath) && File.Exists(publicKeyPath))
    {
        pem = File.ReadAllText(publicKeyPath);
    }
    else
    {
        if (useEphemeralTestingKey)
        {
            return new RsaSecurityKey(RSA.Create(2048));
        }

        throw new InvalidOperationException(
            "JWT public key not configured. Set JWT_PUBLIC_KEY_BASE64 or JWT_PUBLIC_KEY_PATH environment variable.");
    }

    var rsa = System.Security.Cryptography.RSA.Create();
    rsa.ImportFromPem(pem.AsSpan());
    return new RsaSecurityKey(rsa);
}

file static class HealthCheckTags
{
    public static readonly string[] Ready = ["ready"];
}
