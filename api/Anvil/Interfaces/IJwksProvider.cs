using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Anvil.Interfaces
{
    /// <summary>
    /// Supplies the signing keys bearer tokens are validated against.
    /// </summary>
    /// <remarks>
    /// The JWT bearer handler consumes it as its configuration manager, which lets it fetch
    /// keys asynchronously and request a refresh when a token names an unknown key.
    /// </remarks>
    public interface IJwksProvider : IConfigurationManager<OpenIdConnectConfiguration>
    {
        Task<SecurityKey?> GetKeyByIdAsync(string kid);
    }
}
