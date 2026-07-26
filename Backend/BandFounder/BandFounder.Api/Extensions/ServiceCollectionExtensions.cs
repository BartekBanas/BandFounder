using System.Security.Claims;
using System.Text;
using BandFounder.Application.Services;
using BandFounder.Application.Services.Jwt;
using BandFounder.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace BandFounder.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddAuthorizationSwaggerGen(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description =
                    "JWT Authorization header using the Bearer scheme." +
                    "\r\n\r\n Enter only your token below." +
                    "\r\n\r\nExample: \"abc123token\""
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "bearer",
                        Name = "Authorization",
                        In = ParameterLocation.Header
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    public static void AddJwtAuthentication(this IServiceCollection services, JwtConfiguration jwtConfig)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(bearerOptions =>
        {
            bearerOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = jwtConfig.Issuer,
                ValidAudience = jwtConfig.Audience,
                IssuerSigningKey = new SymmetricSecurityKey
                    (Encoding.UTF8.GetBytes(jwtConfig.SecretKey)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
            
            bearerOptions.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var principal = context.Principal;
                    if (principal is null)
                    {
                        context.Fail("Missing principal");
                        return;
                    }

                    var userIdClaim = principal.FindFirst(ClaimTypes.PrimarySid)?.Value;
                    if (!Guid.TryParse(userIdClaim, out var userId))
                    {
                        context.Fail("Missing or invalid user id claim");
                        return;
                    }

                    var versionClaim = principal.FindFirst(AuthClaimTypes.PasswordVersion)?.Value;
                    // Tokens issued before PasswordVersion existed are treated as version 0.
                    var tokenVersion = int.TryParse(versionClaim, out var parsedVersion) ? parsedVersion : 0;

                    var dbContext = context.HttpContext.RequestServices
                        .GetRequiredService<BandFounderDbContext>();

                    var currentVersion = await dbContext.Accounts
                        .AsNoTracking()
                        .Where(account => account.Id == userId)
                        .Select(account => (int?)account.PasswordVersion)
                        .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                    if (currentVersion is null || currentVersion.Value != tokenVersion)
                    {
                        context.Fail("Token has been revoked");
                    }
                },
                OnChallenge = _ => throw new UnauthorizedAccessException("You need to be logged in to perform this action.")
            };
        });
    }

    public static void AddCorsPolicies(this IServiceCollection services, IConfiguration configuration)
    {
        var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        var devOrigins = new List<string> { "http://127.0.0.1:3000", "http://127.0.0.1:3001" };
        if (configuredOrigins.Length > 0)
        {
            foreach (var origin in configuredOrigins)
            {
                if (!devOrigins.Contains(origin)) devOrigins.Add(origin);
            }
        }

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicies.LocalDevelopment, policy =>
            {
                policy.WithOrigins(devOrigins.ToArray())
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });

            options.AddPolicy(CorsPolicies.Production, policy =>
            {
                if (configuredOrigins.Length > 0)
                {
                    policy.WithOrigins(configuredOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
                else
                {
                    // Intentionally keep restrictive when no production origins are configured.
                }
            });
        });
    }
}