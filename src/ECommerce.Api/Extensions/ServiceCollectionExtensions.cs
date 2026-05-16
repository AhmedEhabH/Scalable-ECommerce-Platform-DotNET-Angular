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

namespace ECommerce.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<ILoggerService, LoggerService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ProductService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<IProductService>(sp => new CachedProductService(sp.GetRequiredService<ProductService>(), sp.GetRequiredService<ICacheService>()));
        services.AddScoped<ICategoryService>(sp => new CachedCategoryService(sp.GetRequiredService<CategoryService>(), sp.GetRequiredService<ICacheService>()));
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IReviewService, ReviewService>();
        
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
