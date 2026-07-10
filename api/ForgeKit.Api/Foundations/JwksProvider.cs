using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ForgeKit.Api.Interfaces;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Concurrent;

namespace ForgeKit.Api.Foundations
{
    public class JwksObject
    {
        public Key[]? Keys { get; set; }
    }

    public class Key
    {
        public string? Kty { get; set; }
        public string? N { get; set; }
        public string? E { get; set; }
        public string? Kid { get; set; }
    }

    public class JwksProvider(string jwksUrl) : IJwksProvider
    {
        private readonly string _jwksUrl = jwksUrl;
        private readonly HttpClient _httpClient = new();
        private readonly ConcurrentDictionary<string, SecurityKey> _keyCache = new();

        public async Task<SecurityKey?> GetKeyByIdAsync(string kid)
        {
            if (_keyCache.TryGetValue(kid, out var cachedKey))
            {
                return cachedKey;
            }

            var jwksJson = await _httpClient.GetStringAsync(_jwksUrl);
            var jwks = JsonConvert.DeserializeObject<JwksObject>(jwksJson);

            if (jwks?.Keys == null) return null;

            foreach (var key in jwks.Keys)
            {
                if (key?.Kid == null || key.N == null || key.E == null) continue;

                var rsaParams = new RSAParameters
                {
                    Modulus = Base64UrlTextEncoder.Decode(key.N),
                    Exponent = Base64UrlTextEncoder.Decode(key.E)
                };

                var rsaKey = new RsaSecurityKey(rsaParams)
                {
                    KeyId = key.Kid
                };

                _keyCache.TryAdd(key.Kid, rsaKey);
            }

            return _keyCache.TryGetValue(kid, out var resultKey) ? resultKey : null;
        }
    }
}
