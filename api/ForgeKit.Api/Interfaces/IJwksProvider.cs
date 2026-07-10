using Microsoft.IdentityModel.Tokens;

namespace ForgeKit.Api.Interfaces
{
    public interface IJwksProvider
    {
        Task<SecurityKey?> GetKeyByIdAsync(string kid);
    }
}
