using System.Text;
using BandFounder.Application.Services.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
            
            // Custom message for 401 Unauthorized responses
            bearerOptions.Events = new JwtBearerEvents
            {
                OnChallenge = _ => throw new UnauthorizedAccessException("You need to be logged in to perform this action.")
            };
        });
    }

    public static void AddCorsPolicies(this IServiceCollection services, IConfiguration configuration)
    {
        var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        var devOrigins = new List<string> { "http://localhost:3000", "http://localhost:3001" };
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