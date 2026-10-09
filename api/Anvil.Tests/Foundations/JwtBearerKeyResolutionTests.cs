using Anvil.Extensions;
using Anvil.Foundations;
using Anvil.Interfaces;
using Anvil.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace Anvil.Tests.Foundations;

/// <summary>
/// Runs tokens through the real <see cref="JwtBearerHandler"/> configured by
/// <see cref="ConfigureJwtBearerOptions"/>, so the test fails if the handler stops taking its
/// keys from <see cref="IJwksProvider"/> — which would reject every authenticated request.
/// </summary>
public sealed class JwtBearerKeyResolutionTests : IDisposable
{
    private const string Issuer = "https://auth.test";
    private const string Audience = "https://api.test";

    private readonly FakeJwksServer _server = new();
    private readonly TestSigningKey _published = new("published");
    private readonly TestSigningKey _unpublished = new("unpublished");
    private readonly ServiceProvider _services;

    public JwtBearerKeyResolutionTests()
    {
        _server.Document = TestSigningKey.Jwks(_published);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.Configure<JwtSetupData>(o =>
        {
            o.Issuer = Issuer;
            o.Audience = Audience;
        });
        services.AddSingleton<IJwksProvider>(
            new JwksProvider("https://auth.test/api/auth/jwks", new HttpClient(_server)));
        services.AddTransient<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        _services = services.BuildServiceProvider();
    }

    [Fact]
    public async Task ATokenSignedByAPublishedKey_IsAuthenticated()
    {
        var result = await AuthenticateAsync(CreateToken(_published));

        result.Succeeded.ShouldBeTrue(result.Failure?.ToString());
        result.Principal!.FindFirst("sub")?.Value.ShouldBe("user-1");
        _server.Requests.ShouldBe(1);
    }

    [Fact]
    public async Task ATokenSignedByAnUnpublishedKey_IsRejected()
    {
        // Establish the path works with a good token first, so the rejection below is
        // about the key and not about a misconfigured handler.
        (await AuthenticateAsync(CreateToken(_published))).Succeeded.ShouldBeTrue();

        var result = await AuthenticateAsync(CreateToken(_unpublished));

        result.Succeeded.ShouldBeFalse();
        result.Failure.ShouldBeOfType<SecurityTokenSignatureKeyNotFoundException>();
    }

    [Fact]
    public async Task RepeatedUnknownKeys_DoNotTriggerRepeatedDownloads()
    {
        (await AuthenticateAsync(CreateToken(_published))).Succeeded.ShouldBeTrue();

        for (var i = 0; i < 20; i++)
        {
            (await AuthenticateAsync(CreateToken(_unpublished))).Succeeded.ShouldBeFalse();
        }

        // The first fetch was just now, so no early refresh is due yet.
        _server.Requests.ShouldBe(1);
    }

    private string CreateToken(TestSigningKey key) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Claims = new Dictionary<string, object> { ["sub"] = "user-1" },
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = key.Credentials,
        });

    private async Task<AuthenticateResult> AuthenticateAsync(string token)
    {
        using var scope = _services.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Headers.Authorization = $"Bearer {token}";
        return await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
    }

    public void Dispose()
    {
        _services.Dispose();
        _published.Dispose();
        _unpublished.Dispose();
    }
}
