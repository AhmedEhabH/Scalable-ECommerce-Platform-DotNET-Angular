using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using ECommerce.Application.Auth.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;
    private readonly RsaSecurityKey _rsaPrivateKey;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _accessTokenExpirationMinutes = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 60);
        _refreshTokenExpirationDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);
        _rsaPrivateKey = LoadRsaPrivateKey();
    }

    public string GenerateAccessToken(Guid userId, string email, string role)
    {
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(_rsaPrivateKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public DateTime GetTokenExpiration()
    {
        return DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes);
    }

    private static RsaSecurityKey LoadRsaPrivateKey()
    {
        var privateKeyBase64 = Environment.GetEnvironmentVariable("JWT_PRIVATE_KEY_BASE64");
        var privateKeyPath = Environment.GetEnvironmentVariable("JWT_PRIVATE_KEY_PATH");
        string pem;

        if (!string.IsNullOrEmpty(privateKeyBase64))
        {
            pem = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(privateKeyBase64));
        }
        else if (!string.IsNullOrEmpty(privateKeyPath) && File.Exists(privateKeyPath))
        {
            pem = File.ReadAllText(privateKeyPath);
        }
        else
        {
            throw new InvalidOperationException(
                "JWT private key not configured. Set JWT_PRIVATE_KEY_BASE64 or JWT_PRIVATE_KEY_PATH environment variable.");
        }

        var rsa = RSA.Create();
        rsa.ImportFromPem(pem.AsSpan());
        return new RsaSecurityKey(rsa);
    }
}
