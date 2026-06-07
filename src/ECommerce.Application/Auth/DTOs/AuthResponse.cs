namespace ECommerce.Application.Auth.DTOs;

/// <summary>
/// Authentication response containing tokens and user info
/// </summary>
/// <param name="UserId">User unique identifier</param>
/// <param name="AccessToken">JWT access token for authenticated requests</param>
/// <param name="RefreshToken">Refresh token for obtaining new access tokens</param>
/// <param name="ExpiresAt">Access token expiration date/time</param>
/// <param name="Email">User email address</param>
/// <param name="FullName">User full name</param>
/// <param name="Role">User role (Customer, Seller, Admin)</param>
public record AuthResponse(
    string UserId,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string Email,
    string FullName,
    string Role
);
