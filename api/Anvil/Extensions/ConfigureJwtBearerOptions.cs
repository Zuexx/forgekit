using Anvil.Interfaces;
using Anvil.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Anvil.Extensions
{
    public class ConfigureJwtBearerOptions(
        IConfiguration config,
        IJwksProvider jwksProvider,
        IOptions<JwtSetupData> jwtOptions) : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly IConfiguration _config = config;
        private readonly IJwksProvider _jwksProvider = jwksProvider;
        private readonly JwtSetupData _jwtData = jwtOptions.Value;

        public void Configure(string? name, JwtBearerOptions options)
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;

            // Keys come from the JWKS provider acting as the handler's configuration manager:
            // the handler awaits it before validating, and asks it to refresh when a token
            // names a key it does not hold. The provider rate-limits those refreshes.
            options.ConfigurationManager = _jwksProvider;
            options.RefreshOnIssuerKeyNotFound = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtData.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtData.Audience,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }

                    return Task.CompletedTask;
                }
            };
        }

        public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);
    }
}
