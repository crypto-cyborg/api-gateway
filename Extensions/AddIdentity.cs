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
                options.TokenValidationParameters = GetValidationParameters(configuration);

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = async (context) =>
                    {
                        var cookieName = configuration["Cookies:Name"]!;
                        var accessToken = context.Request.Cookies[cookieName];
                        
                        context.Token = accessToken;

                        if (accessToken is not null)
                        {
                            var validationParams = GetValidationParameters(configuration);
                            var handler = new JwtSecurityTokenHandler();
                            var principal = await handler.ValidateTokenAsync(accessToken, validationParams);

                            var userId = principal.Claims
                                .FirstOrDefault(c => c.Key == "userId").Value;
                            
                            context.Request.Headers.Append("userId", userId.ToString());
                        }
                    }
                };
            });

        services.AddAuthorization();
    }

    private static TokenValidationParameters GetValidationParameters(IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(nameof(JwtOptions)).Get<JwtOptions>();

        if (jwtOptions is null) throw new InvalidConfigurationException("Jwt configurations not found");

        return new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecretKey)
            ),
            RoleClaimType = ClaimTypes.Role,
        };
    }
}