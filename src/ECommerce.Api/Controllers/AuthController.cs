using ECommerce.Api.Models;
using ECommerce.Application.Auth.DTOs;
using ECommerce.Application.Auth.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Authentication endpoints for user login, registration, and token management
/// </summary>
[EnableRateLimiting("auth")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    private void SetAccessTokenCookie(string token)
    {
        var expirationMinutes = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 60);
        Response.Cookies.Append("access_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
        });
    }

    /// <summary>
    /// Authenticate user and return JWT tokens
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/login
    ///     {
    ///         "email": "user@example.com",
    ///         "password": "SecurePass123!"
    ///     }
    /// </remarks>
    /// <param name="request">Login credentials</param>
    /// <returns>User info and refresh token (access token set in HttpOnly cookie)</returns>
    /// <response code="200">Returns user info and refresh token</response>
    /// <response code="401">Invalid credentials</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            SetAccessTokenCookie(result.AccessToken);
            return HandleSuccess(new
            {
                result.UserId,
                result.RefreshToken,
                result.ExpiresAt,
                result.Email,
                result.FullName,
                result.Role
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return HandleUnauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/register
    ///     {
    ///         "email": "newuser@example.com",
    ///         "password": "SecurePass123!",
    ///         "firstName": "John",
    ///         "lastName": "Doe",
    ///         "phoneNumber": "+1234567890"
    ///     }
    /// </remarks>
    /// <param name="request">Registration details</param>
    /// <returns>User info and refresh token (access token set in HttpOnly cookie)</returns>
    /// <response code="201">User created successfully</response>
    /// <response code="400">Validation error or user already exists</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<object>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            SetAccessTokenCookie(result.AccessToken);
            return HandleCreated(new
            {
                result.UserId,
                result.RefreshToken,
                result.ExpiresAt,
                result.Email,
                result.FullName,
                result.Role
            });
        }
        catch (InvalidOperationException ex)
        {
            return HandleBadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Refresh an expired access token using a valid refresh token
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/refresh-token
    ///     {
    ///         "refreshToken": "your-refresh-token-here"
    ///     }
    /// </remarks>
    /// <param name="request">Refresh token</param>
    /// <returns>New user info and refresh token (new access token set in HttpOnly cookie)</returns>
    /// <response code="200">Returns new user info and refresh token</response>
    /// <response code="401">Invalid or expired refresh token</response>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            SetAccessTokenCookie(result.AccessToken);
            return HandleSuccess(new
            {
                result.UserId,
                result.RefreshToken,
                result.ExpiresAt,
                result.Email,
                result.FullName,
                result.Role
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return HandleUnauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Logout user by clearing the access token cookie
    /// </summary>
    /// <response code="200">Logged out successfully</response>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public IActionResult Logout()
    {
        Response.Cookies.Append("access_token", "", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(-1)
        });
        return HandleOkWithMessage("Logged out successfully");
    }
}
