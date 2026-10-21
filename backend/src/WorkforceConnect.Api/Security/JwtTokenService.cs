using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WorkforceConnect.Application.Common;
using WorkforceConnect.Application.Interfaces;

namespace WorkforceConnect.Api.Security;

public sealed class JwtTokenService(IOptions<JwtSettings> options, TimeProvider clock) : IJwtTokenService
{
    public AccessToken CreateToken(int userId, string email, string roleName)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(settings.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(AppClaims.UserId, userId.ToString()),
                new Claim(AppClaims.Email, email),
                new Claim(AppClaims.Role, roleName),
                new Claim("jti", Guid.NewGuid().ToString("N"))
            }),
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            NotBefore = now,
            IssuedAt = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AccessToken(token, expires);
    }
}
