using ForgeKit.Api.Interfaces;
using ForgeKit.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ForgeKit.Api.Extensions
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

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtData.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtData.Audience,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                {
                    var keys = new List<SecurityKey>();

                    var key = _jwksProvider.GetKeyByIdAsync(kid).GetAwaiter().GetResult();
                    if (key != null)
                    {
                        keys.Add(key);
                    }

                    return keys;
                }
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
