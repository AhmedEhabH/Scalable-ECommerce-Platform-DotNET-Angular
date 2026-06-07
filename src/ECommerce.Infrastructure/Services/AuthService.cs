using ECommerce.Application.Auth.DTOs;
using ECommerce.Application.Auth.Interfaces;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeService _dateTimeService;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _dateTimeService = dateTimeService;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        var user = User.Create(
            email: request.Email,
            passwordHash: _passwordHasher.HashPassword(request.Password),
            firstName: request.FirstName,
            lastName: request.LastName,
            phoneNumber: request.PhoneNumber
        );

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.IsActive &&
                u.RefreshTokens.Any(rt => rt.Token == refreshToken && !rt.IsExpired && !rt.IsRevoked));

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var userRefreshToken = user.RefreshTokens.First(rt => rt.Token == refreshToken);
        userRefreshToken.Revoke();

        return await GenerateAuthResponse(user);
    }

    private async Task<AuthResponse> GenerateAuthResponse(User user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var expiresAt = _tokenService.GetTokenExpiration();

        var refreshTokenEntity = RefreshToken.Create(
            user.Id,
            newRefreshToken,
            _dateTimeService.UtcNow.AddDays(7)
        );
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return new AuthResponse(
            UserId: user.Id.ToString(),
            AccessToken: accessToken,
            RefreshToken: newRefreshToken,
            ExpiresAt: expiresAt,
            Email: user.Email,
            FullName: user.FullName,
            Role: user.Role.ToString()
        );
    }
}
