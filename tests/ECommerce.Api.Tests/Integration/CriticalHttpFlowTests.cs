using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Services;
using FluentAssertions;
using Hangfire;
using MassTransit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ECommerce.Api.Tests.Integration;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CriticalHttpFlowCollection
{
    public const string Name = "Critical HTTP flow tests";
}

[Collection(CriticalHttpFlowCollection.Name)]
public class CriticalHttpFlowTests
{
    [Fact]
    public async Task AuthCookie_ProtectsEndpoints_EnforcesRoles_AndRefreshes()
    {
        using var factory = new CriticalHttpFactory();
        using var anonymousClient = factory.CreateClient(CreateClientOptions());
        await factory.SeedAsync();

        var (userClient, loginResponse, refreshToken) = await LoginAsync(factory, "user@test.local");
        using (userClient)
        {
            var setCookie = loginResponse.Headers.GetValues("Set-Cookie").Single();
            setCookie.Should().Contain("access_token=");
            var normalizedCookie = setCookie.ToLowerInvariant();
            normalizedCookie.Should().Contain("httponly");
            normalizedCookie.Should().Contain("secure");
            normalizedCookie.Should().Contain("samesite=none");

            (await userClient.GetAsync("/api/cart")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await anonymousClient.GetAsync("/api/cart")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await userClient.GetAsync("/api/admin/dashboard")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var refreshResponse = await anonymousClient.PostAsJsonAsync(
                "/api/auth/refresh-token",
                new { refreshToken });

            var refreshBody = await refreshResponse.Content.ReadAsStringAsync();
            refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK, "refresh response was: {0}", refreshBody);
            refreshResponse.Headers.GetValues("Set-Cookie").Single().Should().Contain("access_token=");
        }
    }

    [Fact]
    public async Task CartCheckout_CreatesOrder_DeductsStock_ClearsCart_AndEnforcesOwnership()
    {
        using var factory = new CriticalHttpFactory();
        factory.CreateClient(CreateClientOptions()).Dispose();
        await factory.SeedAsync();
        var (userClient, _, _) = await LoginAsync(factory, "user@test.local");
        using (userClient)
        {
            var addResponse = await userClient.PostAsJsonAsync(
                "/api/cart/items",
                new { productId = factory.ProductId, quantity = 2 });
            addResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var checkoutResponse = await userClient.PostAsJsonAsync("/api/checkout", ValidCheckoutRequest());
            checkoutResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var orderId = await ReadDataGuidAsync(checkoutResponse, "id");

            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                (await context.Products.SingleAsync(p => p.Id == factory.ProductId)).StockQuantity.Should().Be(8);
                (await context.Carts.Include(c => c.Items).SingleAsync(c => c.UserId == factory.UserId))
                    .Items.Should().BeEmpty();
                (await context.Orders.SingleAsync(o => o.Id == orderId)).UserId.Should().Be(factory.UserId);
            }

            var cartResponse = await userClient.GetAsync("/api/cart");
            cartResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            (await ReadDataBooleanAsync(cartResponse, "isEmpty")).Should().BeTrue();

            var (otherClient, _, _) = await LoginAsync(factory, "other@test.local");
            using (otherClient)
            {
                (await otherClient.GetAsync($"/api/orders/{orderId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            }
        }
    }

    [Fact]
    public async Task Checkout_WhenStockDropsAfterCarting_IsRejectedWithoutCreatingOrder()
    {
        using var factory = new CriticalHttpFactory();
        factory.CreateClient(CreateClientOptions()).Dispose();
        await factory.SeedAsync();
        var (userClient, _, _) = await LoginAsync(factory, "user@test.local");
        using (userClient)
        {
            (await userClient.PostAsJsonAsync(
                "/api/cart/items",
                new { productId = factory.ProductId, quantity = 2 })).StatusCode.Should().Be(HttpStatusCode.OK);

            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var product = await context.Products.SingleAsync(p => p.Id == factory.ProductId);
                product.SetStock(1);
                await context.SaveChangesAsync();
            }

            var response = await userClient.PostAsJsonAsync("/api/checkout", ValidCheckoutRequest());

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().Contain("Insufficient stock");
            await using var verifyScope = factory.Services.CreateAsyncScope();
            (await verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Orders.CountAsync())
                .Should().Be(0);
        }
    }

    [Fact]
    public async Task ProductHttpFlow_CoversAdminCrud_SellerOwnership_AndStructuredValidation()
    {
        using var factory = new CriticalHttpFactory();
        factory.CreateClient(CreateClientOptions()).Dispose();
        await factory.SeedAsync();
        var (adminClient, _, _) = await LoginAsync(factory, "admin@test.local");
        using (adminClient)
        {
            var invalidResponse = await adminClient.PostAsJsonAsync("/api/products", new
            {
                categoryId = Guid.Empty,
                name = "",
                slug = "",
                price = 0,
                sku = "",
                stockQuantity = -1
            });
            invalidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var invalidBody = await invalidResponse.Content.ReadAsStringAsync();
            invalidBody.Should().Contain("\"success\":false");
            invalidBody.Should().Contain("\"errors\"");

            var createResponse = await adminClient.PostAsJsonAsync("/api/products", new
            {
                categoryId = factory.CategoryId,
                name = "HTTP Product",
                slug = "http-product",
                price = 45.50m,
                sku = "HTTP-001",
                stockQuantity = 6
            });
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdId = await ReadDataGuidAsync(createResponse, "id");

            var updateResponse = await adminClient.PutAsJsonAsync(
                $"/api/products/{createdId}",
                new { name = "Updated HTTP Product", price = 40m });
            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            (await updateResponse.Content.ReadAsStringAsync()).Should().Contain("Updated HTTP Product");

            var deleteResponse = await adminClient.DeleteAsync($"/api/products/{createdId}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var (sellerClient, _, _) = await LoginAsync(factory, "seller@test.local");
        using (sellerClient)
        {
            var response = await sellerClient.PutAsJsonAsync(
                $"/api/products/{factory.OtherSellerProductId}",
                new { name = "Stolen Product" });
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task OrderStatusHttpFlow_AllowsValid_RejectsInvalid_AndRequiresAdmin()
    {
        using var factory = new CriticalHttpFactory();
        factory.CreateClient(CreateClientOptions()).Dispose();
        await factory.SeedAsync();
        var orderId = await factory.SeedPendingOrderAsync();
        var (adminClient, _, _) = await LoginAsync(factory, "admin@test.local");
        using (adminClient)
        {
            var valid = await adminClient.PutAsJsonAsync(
                $"/api/admin/orders/{orderId}/status",
                new { status = OrderStatus.Confirmed });
            valid.StatusCode.Should().Be(HttpStatusCode.OK);

            var invalid = await adminClient.PutAsJsonAsync(
                $"/api/admin/orders/{orderId}/status",
                new { status = OrderStatus.Shipped });
            invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        var (userClient, _, _) = await LoginAsync(factory, "user@test.local");
        using (userClient)
        {
            (await userClient.PutAsJsonAsync(
                $"/api/admin/orders/{orderId}/status",
                new { status = OrderStatus.Processing })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }

    private static WebApplicationFactoryClientOptions CreateClientOptions() => new()
    {
        AllowAutoRedirect = false,
        HandleCookies = false
    };

    private static object ValidCheckoutRequest() => new
    {
        shippingAddress = new
        {
            street = "1 Main Street",
            city = "Cairo",
            state = "Cairo",
            postalCode = "11511",
            country = "Egypt"
        },
        notes = "HTTP integration test"
    };

    private static async Task<(HttpClient Client, HttpResponseMessage Response, string RefreshToken)> LoginAsync(
        CriticalHttpFactory factory,
        string email)
    {
        var client = factory.CreateClient(CreateClientOptions());
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = CriticalHttpFactory.Password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cookie = response.Headers.GetValues("Set-Cookie").Single().Split(';', 2)[0];
        client.DefaultRequestHeaders.Add("Cookie", cookie);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var refreshToken = document.RootElement.GetProperty("data").GetProperty("refreshToken").GetString();
        return (client, response, refreshToken.Should().NotBeNullOrWhiteSpace().And.Subject!);
    }

    private static async Task<Guid> ReadDataGuidAsync(HttpResponseMessage response, string property)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").GetProperty(property).GetGuid();
    }

    private static async Task<bool> ReadDataBooleanAsync(HttpResponseMessage response, string property)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").GetProperty(property).GetBoolean();
    }
}

public sealed class CriticalHttpFactory : WebApplicationFactory<Program>
{
    public const string Password = "Testing123!";

    private readonly string _databaseName = $"critical-http-{Guid.NewGuid():N}";
    private readonly string _privateKeyBase64;
    private readonly string _publicKeyBase64;
    private bool _seeded;

    public Guid UserId { get; } = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public Guid OtherUserId { get; } = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public Guid AdminId { get; } = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public Guid SellerId { get; } = Guid.Parse("10000000-0000-0000-0000-000000000004");
    public Guid OtherSellerId { get; } = Guid.Parse("10000000-0000-0000-0000-000000000005");
    public Guid CategoryId { get; } = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public Guid ProductId { get; } = Guid.Parse("30000000-0000-0000-0000-000000000001");
    public Guid OtherSellerProductId { get; } = Guid.Parse("30000000-0000-0000-0000-000000000002");

    public CriticalHttpFactory()
    {
        using var rsa = RSA.Create(2048);
        _privateKeyBase64 = ToBase64(rsa.ExportRSAPrivateKeyPem());
        _publicKeyBase64 = ToBase64(rsa.ExportSubjectPublicKeyInfoPem());
        Environment.SetEnvironmentVariable("JWT_PRIVATE_KEY_BASE64", _privateKeyBase64);
        Environment.SetEnvironmentVariable("JWT_PUBLIC_KEY_BASE64", _publicKeyBase64);
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName)
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
            services.AddHangfire(config => config.UseInMemoryStorage());
            services.AddMassTransitTestHarness();
        });
    }

    public async Task SeedAsync()
    {
        if (_seeded)
            return;

        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = new PasswordHasher();

        var user = CreateUser(UserId, "user@test.local", Role.User, passwordHasher);
        var otherUser = CreateUser(OtherUserId, "other@test.local", Role.User, passwordHasher);
        var admin = CreateUser(AdminId, "admin@test.local", Role.Admin, passwordHasher);
        var seller = CreateUser(SellerId, "seller@test.local", Role.Seller, passwordHasher);
        var otherSeller = CreateUser(OtherSellerId, "other-seller@test.local", Role.Seller, passwordHasher);
        context.Users.AddRange(user, otherUser, admin, seller, otherSeller);

        var category = Category.Create("HTTP Category", "http-category");
        TestDatabaseFixture.SetPrivateProperty(category, "Id", CategoryId);
        context.Categories.Add(category);

        var sellerVendor = Vendor.Create(SellerId, "Seller One", contactEmail: "seller@test.local");
        var otherVendor = Vendor.Create(OtherSellerId, "Seller Two", contactEmail: "other-seller@test.local");
        sellerVendor.Approve();
        otherVendor.Approve();
        context.Vendors.AddRange(sellerVendor, otherVendor);

        var product = Product.Create(null, CategoryId, "Checkout Product", "checkout-product", 25m, "CHECKOUT-001", 10);
        TestDatabaseFixture.SetPrivateProperty(product, "Id", ProductId);
        var otherSellerProduct = Product.Create(
            otherVendor.Id,
            CategoryId,
            "Other Seller Product",
            "other-seller-product",
            35m,
            "OTHER-001",
            10);
        TestDatabaseFixture.SetPrivateProperty(otherSellerProduct, "Id", OtherSellerProductId);
        context.Products.AddRange(product, otherSellerProduct);

        await context.SaveChangesAsync();
        _seeded = true;
    }

    public async Task<Guid> SeedPendingOrderAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var order = Order.Create(
            UserId,
            25m,
            2.5m,
            10m,
            0m,
            ECommerce.Domain.ValueObjects.Address.Create("1 Main Street", "Cairo", "Cairo", "11511", "Egypt"));
        order.AddItem(ProductId, Guid.Empty, "Checkout Product", "CHECKOUT-001", 25m, 1);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return order.Id;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Environment.SetEnvironmentVariable("JWT_PRIVATE_KEY_BASE64", null);
        Environment.SetEnvironmentVariable("JWT_PUBLIC_KEY_BASE64", null);
    }

    private static User CreateUser(Guid id, string email, Role role, PasswordHasher passwordHasher)
    {
        var user = User.Create(email, passwordHasher.HashPassword(Password), "Test", role.ToString(), role: role);
        TestDatabaseFixture.SetPrivateProperty(user, "Id", id);
        user.ConfirmEmail();
        return user;
    }

    private static string ToBase64(string pem) => Convert.ToBase64String(Encoding.UTF8.GetBytes(pem));
}
