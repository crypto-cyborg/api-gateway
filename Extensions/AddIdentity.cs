using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiGateway.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Extensions;

public static partial class ApiExtensions
{
    public static void AddGatewayIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = GetValidationParameters(configuration, true);

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = (context) =>
                    {
                        var cookieName = configuration["Cookies:Name"]!;
                        var accessToken = context.Request.Cookies[cookieName];
                        
                        context.Token = accessToken;

                        if (accessToken is null) return Task.CompletedTask;
                        
                        var validationParams = GetValidationParameters(configuration, lifetime: false);
                        var handler = new JwtSecurityTokenHandler();
                        var principal = handler.ValidateToken(accessToken, validationParams, out _);

                        // TODO: This can be null
                        var userId = principal .FindFirst("userId")!.Value;
                            
                        context.Request.Headers.Append("userId", userId);

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
    }

    private static TokenValidationParameters GetValidationParameters(IConfiguration configuration, bool lifetime)
    {
        var jwtOptions = configuration.GetSection(nameof(JwtOptions)).Get<JwtOptions>();

        if (jwtOptions is null) throw new InvalidConfigurationException("Jwt configurations not found");

        return new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = lifetime,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecretKey)
            ),
            RoleClaimType = ClaimTypes.Role,
        };
    }
}