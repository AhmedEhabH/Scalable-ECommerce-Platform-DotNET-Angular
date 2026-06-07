using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using ECommerce.Application.Auth.DTOs;
using ECommerce.Application.Auth.Interfaces;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using FluentAssertions;

namespace ECommerce.Api.Tests.Integration;

public class JwtRoleTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;

    public JwtRoleTests()
    {
        _context = TestDatabaseFixture.CreateInMemoryContext(Guid.NewGuid().ToString());

        var (privateKeyBase64, _) = GenerateRsaKeyPair();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "ECommerceApi",
                ["Jwt:Audience"] = "ECommerceClient",
                ["Jwt:AccessTokenExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            }!)
            .Build();

        Environment.SetEnvironmentVariable("JWT_PRIVATE_KEY_BASE64", privateKeyBase64);
        _tokenService = new TokenService(config);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        Environment.SetEnvironmentVariable("JWT_PRIVATE_KEY_BASE64", null);
    }

    private static (string privateKeyBase64, string publicKeyBase64) GenerateRsaKeyPair()
    {
        using var rsa = RSA.Create(2048);
        var privateKeyPem = rsa.ExportRSAPrivateKeyPem();
        var publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
        return (
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(privateKeyPem)),
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(publicKeyPem))
        );
    }

    [Fact]
    public void GenerateAccessToken_AdminRole_ContainsAdminRoleClaim()
    {
        var userId = Guid.NewGuid();
        var token = _tokenService.GenerateAccessToken(userId, "admin@test.com", "Admin");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public void GenerateAccessToken_SellerRole_ContainsSellerRoleClaim()
    {
        var userId = Guid.NewGuid();
        var token = _tokenService.GenerateAccessToken(userId, "seller@test.com", "Seller");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Seller");
    }

    [Fact]
    public void GenerateAccessToken_UserRole_ContainsUserRoleClaim()
    {
        var userId = Guid.NewGuid();
        var token = _tokenService.GenerateAccessToken(userId, "user@test.com", "User");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "User");
    }

    [Fact]
    public void GenerateAccessToken_ContainsRequiredClaims()
    {
        var userId = Guid.NewGuid();
        var token = _tokenService.GenerateAccessToken(userId, "test@test.com", "Admin");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "test@test.com");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateAccessToken_ProducesValidToken()
    {
        var userId = Guid.NewGuid();
        var token = _tokenService.GenerateAccessToken(userId, "test@test.com", "Admin");

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Length.Should().Be(3);
    }

    [Fact]
    public async Task Login_AdminUser_TokenContainsAdminRole()
    {
        var passwordHasher = new PasswordHasher();
        var admin = User.Create(
            "admin@test.com",
            passwordHasher.HashPassword("Admin@123"),
            "Admin",
            "User",
            role: Role.Admin
        );
        admin.ConfirmEmail();
        admin.Activate();
        _context.Users.Add(admin);
        await _context.SaveChangesAsync();

        var dateTimeService = new DateTimeService();
        var authService = new AuthService(_context, passwordHasher, _tokenService, dateTimeService);

        var result = await authService.LoginAsync(new LoginRequest("admin@test.com", "Admin@123"));

        result.Role.Should().Be("Admin");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.AccessToken);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public async Task Login_SellerUser_TokenContainsSellerRole()
    {
        var passwordHasher = new PasswordHasher();
        var seller = User.Create(
            "seller@test.com",
            passwordHasher.HashPassword("Seller@123"),
            "Seller",
            "User",
            role: Role.Seller
        );
        seller.ConfirmEmail();
        seller.Activate();
        _context.Users.Add(seller);
        await _context.SaveChangesAsync();

        var dateTimeService = new DateTimeService();
        var authService = new AuthService(_context, passwordHasher, _tokenService, dateTimeService);

        var result = await authService.LoginAsync(new LoginRequest("seller@test.com", "Seller@123"));

        result.Role.Should().Be("Seller");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.AccessToken);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Seller");
    }

    [Fact]
    public async Task Login_StandardUser_TokenContainsUserRole()
    {
        var passwordHasher = new PasswordHasher();
        var user = User.Create(
            "customer@test.com",
            passwordHasher.HashPassword("Customer@123"),
            "Customer",
            "User",
            role: Role.User
        );
        user.ConfirmEmail();
        user.Activate();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dateTimeService = new DateTimeService();
        var authService = new AuthService(_context, passwordHasher, _tokenService, dateTimeService);

        var result = await authService.LoginAsync(new LoginRequest("customer@test.com", "Customer@123"));

        result.Role.Should().Be("User");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.AccessToken);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "User");
    }
}
