using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Anvil.Tests.Foundations;

/// <summary>A clock the test advances by hand.</summary>
internal sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}

/// <summary>
/// Serves whatever JWKS document the test currently publishes and counts the downloads.
/// </summary>
internal sealed class FakeJwksServer : HttpMessageHandler
{
    private int _requests;

    public string Document { get; set; } = """{"keys":[]}""";

    public bool Fail { get; set; }

    public int Requests => Volatile.Read(ref _requests);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _requests);
        if (Fail)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(Document)
        });
    }
}

/// <summary>An RSA signing key and its public JWK, as Better Auth publishes it with RS256.</summary>
internal sealed class TestSigningKey : IDisposable
{
    private readonly RSA _rsa = RSA.Create(2048);

    public TestSigningKey(string kid)
    {
        Kid = kid;
        SecurityKey = new RsaSecurityKey(_rsa) { KeyId = kid };
    }

    public string Kid { get; }

    public RsaSecurityKey SecurityKey { get; }

    public SigningCredentials Credentials => new(SecurityKey, SecurityAlgorithms.RsaSha256);

    public Dictionary<string, string> PublicJwk()
    {
        var parameters = _rsa.ExportParameters(includePrivateParameters: false);
        return new Dictionary<string, string>
        {
            ["kty"] = "RSA",
            ["use"] = "sig",
            ["alg"] = "RS256",
            ["kid"] = Kid,
            ["n"] = Base64UrlEncoder.Encode(parameters.Modulus),
            ["e"] = Base64UrlEncoder.Encode(parameters.Exponent),
        };
    }

    public static string Jwks(params TestSigningKey[] keys) =>
        JsonSerializer.Serialize(new { keys = keys.Select(k => k.PublicJwk()) });

    public void Dispose() => _rsa.Dispose();
}
