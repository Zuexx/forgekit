using Microsoft.IdentityModel.Tokens;

namespace Anvil.Interfaces
{
    public interface IJwksProvider
    {
        Task<SecurityKey?> GetKeyByIdAsync(string kid);
    }
}
