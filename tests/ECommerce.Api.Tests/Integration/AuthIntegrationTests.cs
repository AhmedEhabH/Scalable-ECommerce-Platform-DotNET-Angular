using System.Security.Claims;
using System.Security.Cryptography;
using ECommerce.Application.Auth.DTOs;
using ECommerce.Application.Auth.Interfaces;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;
using FluentAssertions;

namespace ECommerce.Api.Tests.Integration;

public class AuthIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;

    public AuthIntegrationTests()
    {
        _context = TestDatabaseFixture.CreateInMemoryContext(Guid.NewGuid().ToString());

        var (privateKeyBase64, _) = GenerateRsaKeyPair();
        Environment.SetEnvironmentVariable("JWT_PRIVATE_KEY_BASE64", privateKeyBase64);
        
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "ECommerceApi",
                ["Jwt:Audience"] = "ECommerceClient",
                ["Jwt:AccessTokenExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            }!)
            .Build();
            
        var passwordHasher = new PasswordHasher();
        var dateTimeService = new DateTimeService();
        var tokenService = new TokenService(config);
        
        _authService = new AuthService(_context, passwordHasher, tokenService, dateTimeService);
        
        Task.Run(() => TestDatabaseFixture.SeedTestDataAsync(_context)).Wait();
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
    public async Task Register_ShouldCreateUser_WhenDataIsValid()
    {
        var request = new RegisterRequest(
            "newuser@test.com",
            "SecurePass123!",
            "New",
            "User",
            "+9876543210"
        );

        var result = await _authService.RegisterAsync(request);

        result.Should().NotBeNull();
        result.Email.Should().Be("newuser@test.com");
        result.FullName.Should().Be("New User");
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_ShouldThrowException_WhenEmailAlreadyExists()
    {
        var request = new RegisterRequest(
            "test@example.com",
            "SecurePass123!",
            "Test",
            "User",
            "+1234567890"
        );

        var act = () => _authService.RegisterAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Register_ShouldThrowException_WhenEmailIsInvalid()
    {
        var request = new RegisterRequest(
            "invalid-email",
            "SecurePass123!",
            "Test",
            "User"
        );

        var act = () => _authService.RegisterAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Login_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        var request = new LoginRequest("test@example.com", "TestPassword123!");

        var result = await _authService.LoginAsync(request);

        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ShouldThrowException_WhenPasswordIsWrong()
    {
        var request = new LoginRequest("test@example.com", "WrongPassword");

        var act = () => _authService.LoginAsync(request);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid*");
    }

    [Fact]
    public async Task Login_ShouldThrowException_WhenUserDoesNotExist()
    {
        var request = new LoginRequest("nonexistent@test.com", "Password123!");

        var act = () => _authService.LoginAsync(request);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid*");
    }

    [Fact]
    public async Task Login_ShouldIncludeRoleClaim_WhenCredentialsAreValid()
    {
        var request = new LoginRequest("test@example.com", "TestPassword123!");

        var result = await _authService.LoginAsync(request);

        result.AccessToken.Should().NotBeNullOrEmpty();

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.AccessToken);

        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "User");
    }

    [Fact]
    public async Task Register_ShouldIncludeRoleClaim_WhenRegisteringNewUser()
    {
        var request = new RegisterRequest(
            "newroleuser@test.com",
            "SecurePass123!",
            "Role",
            "Test",
            "+1111111111"
        );

        var result = await _authService.RegisterAsync(request);

        result.AccessToken.Should().NotBeNullOrEmpty();

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.AccessToken);

        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "User");
    }

    [Fact]
    public async Task Login_ShouldReturnUserRole_WhenUserIsStandardUser()
    {
        var request = new LoginRequest("test@example.com", "TestPassword123!");

        var result = await _authService.LoginAsync(request);

        result.Role.Should().Be("User");
    }
}