using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WorkforceConnect.Api.Authorization;
using WorkforceConnect.Api.Security;
using WorkforceConnect.Application.Common;
using WorkforceConnect.Application.Interfaces;

namespace WorkforceConnect.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string LoginRateLimitPolicy = "login";
    public const string CorsPolicyName = "Frontend";

    public static IServiceCollection AddApiSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key must be set to a random value of at least 32 characters " +
                "(user secrets or the Jwt__Key environment variable).");
        }

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keep claim names exactly as issued ("sub", "email", "role").
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = AppClaims.Email,
                    RoleClaimType = AppClaims.Role
                };

                options.Events = new JwtBearerEvents
                {
                    // A valid signature is not enough: the account must still exist and be active.
                    OnTokenValidated = async context =>
                    {
                        var db = context.HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();
                        var subject = context.Principal?.FindFirst(AppClaims.UserId)?.Value;

                        var active = int.TryParse(subject, out var userId)
                            && await db.Users.AsNoTracking()
                                .AnyAsync(u => u.Id == userId && u.IsActive, context.HttpContext.RequestAborted);

                        if (!active)
                        {
                            context.Fail("The account no longer exists or is inactive.");
                        }
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            // Secure by default: any endpoint without an explicit rule still requires a signed-in user.
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }

    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return services;
    }

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Slows down password guessing. Behind a reverse proxy, configure forwarded headers
            // so RemoteIpAddress is the real client address.
            options.AddPolicy(LoginRateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }
}
