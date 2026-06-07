using ECommerce.Api.Controllers;
using ECommerce.Api.Models;
using ECommerce.Application.Auth.DTOs;
using ECommerce.Application.Auth.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using FluentAssertions;
using System.Text.Json;

namespace ECommerce.Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly IConfiguration _configuration;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:AccessTokenExpirationMinutes"] = "60"
            })
            .Build();

        _controller = new AuthController(_mockAuthService.Object, _configuration);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region Login Tests

    [Fact]
    public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
    {
        var request = new LoginRequest("user@example.com", "Password123!");
        var authResponse = CreateAuthResponse();

        _mockAuthService
            .Setup(x => x.LoginAsync(request))
            .ReturnsAsync(authResponse);

        var result = await _controller.Login(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var json = JsonSerializer.Serialize(okResult.Value);
        var response = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        response!.Should().ContainKey("Success");
        response["Success"].GetBoolean().Should().BeTrue();
        response.Should().ContainKey("Data");

        var dataJson = response["Data"].GetRawText();
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(dataJson);
        data!.Should().ContainKey("Email");
        data["Email"].GetString().Should().Be("user@example.com");
        data.Should().NotContainKey("AccessToken");
    }

    [Fact]
    public async Task Login_ShouldSetAccessTokenCookie()
    {
        var request = new LoginRequest("user@example.com", "Password123!");
        var authResponse = CreateAuthResponse();

        _mockAuthService
            .Setup(x => x.LoginAsync(request))
            .ReturnsAsync(authResponse);

        await _controller.Login(request);

        var cookie = _controller.Response.Headers["Set-Cookie"].FirstOrDefault();
        cookie.Should().NotBeNull();
        cookie.Should().Contain("access_token=");
        cookie.Should().Contain("httponly");
        cookie.Should().Contain("samesite=none");
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        var request = new LoginRequest("user@example.com", "WrongPassword");

        _mockAuthService
            .Setup(x => x.LoginAsync(request))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password"));

        var result = await _controller.Login(request);

        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenUserNotFound()
    {
        var request = new LoginRequest("nonexistent@example.com", "Password123!");

        _mockAuthService
            .Setup(x => x.LoginAsync(request))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password"));

        var result = await _controller.Login(request);

        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    #endregion

    #region Register Tests

    [Fact]
    public async Task Register_ShouldReturnCreated_WhenRegistrationIsSuccessful()
    {
        var request = new RegisterRequest("newuser@example.com", "Password123!", "John", "Doe", "+1234567890");
        var authResponse = CreateAuthResponse();

        _mockAuthService
            .Setup(x => x.RegisterAsync(request))
            .ReturnsAsync(authResponse);

        var result = await _controller.Register(request);

        var createdResult = result.Should().BeOfType<ObjectResult>().Subject;
        createdResult.StatusCode.Should().Be(201);

        var json = JsonSerializer.Serialize(createdResult.Value);
        var response = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        response!.Should().ContainKey("Success");
        response["Success"].GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenUserAlreadyExists()
    {
        var request = new RegisterRequest("existing@example.com", "Password123!", "John", "Doe");

        _mockAuthService
            .Setup(x => x.RegisterAsync(request))
            .ThrowsAsync(new InvalidOperationException("User with this email already exists"));

        var result = await _controller.Register(request);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region Refresh Token Tests

    [Fact]
    public async Task RefreshToken_ShouldReturnOk_WhenTokenIsValid()
    {
        var request = new RefreshTokenRequest("valid-refresh-token");
        var authResponse = CreateAuthResponse();

        _mockAuthService
            .Setup(x => x.RefreshTokenAsync(request.RefreshToken))
            .ReturnsAsync(authResponse);

        var result = await _controller.RefreshToken(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var json = JsonSerializer.Serialize(okResult.Value);
        var response = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        response!.Should().ContainKey("Success");
        response["Success"].GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnUnauthorized_WhenTokenIsInvalid()
    {
        var request = new RefreshTokenRequest("invalid-refresh-token");

        _mockAuthService
            .Setup(x => x.RefreshTokenAsync(request.RefreshToken))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid refresh token"));

        var result = await _controller.RefreshToken(request);

        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnUnauthorized_WhenTokenIsExpired()
    {
        var request = new RefreshTokenRequest("expired-refresh-token");

        _mockAuthService
            .Setup(x => x.RefreshTokenAsync(request.RefreshToken))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid refresh token"));

        var result = await _controller.RefreshToken(request);

        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public void Logout_ShouldClearCookie()
    {
        var result = _controller.Logout();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var cookie = _controller.Response.Headers["Set-Cookie"].FirstOrDefault();
        cookie.Should().NotBeNull();
        cookie.Should().Contain("access_token=;");
        cookie.Should().Contain("expires=");
    }

    #endregion

    private static AuthResponse CreateAuthResponse()
    {
        return new AuthResponse(
            UserId: "user-123",
            AccessToken: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test-token",
            RefreshToken: "refresh-token-value",
            ExpiresAt: DateTime.UtcNow.AddMinutes(60),
            Email: "user@example.com",
            FullName: "John Doe",
            Role: "User"
        );
    }
}
