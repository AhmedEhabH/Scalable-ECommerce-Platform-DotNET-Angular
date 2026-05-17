using ECommerce.Application.Admin.Interfaces;
using ECommerce.Application.Admin.Services;
using ECommerce.Application.Auth.Interfaces;
using ECommerce.Application.Cart.Interfaces;
using ECommerce.Application.Categories.Interfaces;
using ECommerce.Application.Categories.Services;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders.Interfaces;
using ECommerce.Application.Payments.Interfaces;
using ECommerce.Application.Payments.Services;
using ECommerce.Application.Products.Interfaces;
using ECommerce.Application.Products.Services;
using ECommerce.Application.Reviews.Interfaces;
using ECommerce.Application.Users.Interfaces;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;

namespace ECommerce.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "ECommerce_";
        });
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<ILoggerService, LoggerService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<ProductService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<IProductService>(sp => new CachedProductService(sp.GetRequiredService<ProductService>(), sp.GetRequiredService<ICacheService>(), sp.GetRequiredService<ILogger<CachedProductService>>()));
        services.AddScoped<ICategoryService>(sp => new CachedCategoryService(sp.GetRequiredService<CategoryService>(), sp.GetRequiredService<ICacheService>(), sp.GetRequiredService<ILogger<CachedCategoryService>>()));
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IFileService, LocalFileService>();
        services.AddHttpClient<IAiService, AiChatService>()
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromSeconds(1);
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.MaxDelay = TimeSpan.FromSeconds(10);
                options.Retry.UseJitter = true;

                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 10;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);

                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(25);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
            });
        
        return services;
    }

    public static IServiceCollection AddDatabaseContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, 
                sqlOptions => sqlOptions.EnableRetryOnFailure(3)));

        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        
        return services;
    }
}
