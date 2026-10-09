using Anvil.Foundations;
using Shouldly;

namespace Anvil.Tests.Foundations;

/// <summary>
/// The refresh policy of <see cref="JwksProvider"/>: what bounds outbound requests, and what
/// makes a rotated-out key stop being trusted.
/// </summary>
public sealed class JwksProviderTests : IDisposable
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan MinimumRefreshInterval = TimeSpan.FromSeconds(30);

    private readonly FakeJwksServer _server = new();
    private readonly ManualTimeProvider _clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    private readonly TestSigningKey _keyA = new("key-a");
    private readonly TestSigningKey _keyB = new("key-b");
    private readonly JwksProvider _provider;

    public JwksProviderTests()
    {
        _server.Document = TestSigningKey.Jwks(_keyA);
        _provider = new JwksProvider(
            "https://auth.test/api/auth/jwks",
            new HttpClient(_server),
            _clock,
            RefreshInterval,
            MinimumRefreshInterval);
    }

    [Fact]
    public async Task GetKeyByIdAsync_ReturnsAPublishedKey()
    {
        var key = await _provider.GetKeyByIdAsync("key-a");

        key.ShouldNotBeNull();
        key.KeyId.ShouldBe("key-a");
        _server.Requests.ShouldBe(1);
    }

    [Fact]
    public async Task GetKeyByIdAsync_ForKnownKeys_ServesFromCache()
    {
        for (var i = 0; i < 10; i++)
        {
            (await _provider.GetKeyByIdAsync("key-a")).ShouldNotBeNull();
        }

        _server.Requests.ShouldBe(1);
    }

    [Fact]
    public async Task GetKeyByIdAsync_WithManyUnknownKids_DownloadsAtMostOnceMorePerMinimumInterval()
    {
        // Warm the cache, then let the minimum interval pass so one early refresh is allowed.
        (await _provider.GetKeyByIdAsync("key-a")).ShouldNotBeNull();
        _clock.Advance(MinimumRefreshInterval);

        for (var i = 0; i < 50; i++)
        {
            (await _provider.GetKeyByIdAsync($"forged-{i}")).ShouldBeNull();
        }

        _server.Requests.ShouldBe(2);
    }

    [Fact]
    public async Task GetKeyByIdAsync_WithUnknownKidRightAfterAFetch_DoesNotDownloadAgain()
    {
        (await _provider.GetKeyByIdAsync("key-a")).ShouldNotBeNull();

        (await _provider.GetKeyByIdAsync("forged")).ShouldBeNull();

        _server.Requests.ShouldBe(1);
    }

    [Fact]
    public async Task GetKeyByIdAsync_AfterRotation_NoLongerTrustsTheRemovedKey()
    {
        (await _provider.GetKeyByIdAsync("key-a")).ShouldNotBeNull();

        _server.Document = TestSigningKey.Jwks(_keyB);
        _clock.Advance(RefreshInterval);

        (await _provider.GetKeyByIdAsync("key-a")).ShouldBeNull();
        (await _provider.GetKeyByIdAsync("key-b")).ShouldNotBeNull();
        _server.Requests.ShouldBe(2);
    }

    [Fact]
    public async Task GetKeyByIdAsync_PicksUpANewlyPublishedKeyAfterTheMinimumInterval()
    {
        (await _provider.GetKeyByIdAsync("key-a")).ShouldNotBeNull();

        _server.Document = TestSigningKey.Jwks(_keyA, _keyB);
        _clock.Advance(MinimumRefreshInterval);

        (await _provider.GetKeyByIdAsync("key-b")).ShouldNotBeNull();
        _server.Requests.ShouldBe(2);
    }

    [Fact]
    public async Task GetConfigurationAsync_WhenRefreshFails_KeepsTheCachedSetAndBacksOff()
    {
        (await _provider.GetKeyByIdAsync("key-a")).ShouldNotBeNull();

        _server.Fail = true;
        _clock.Advance(RefreshInterval);

        var configuration = await _provider.GetConfigurationAsync(CancellationToken.None);
        configuration.SigningKeys.ShouldContain(k => k.KeyId == "key-a");
        _server.Requests.ShouldBe(2);

        // Within the back-off window nothing is fetched, however often it is asked.
        await _provider.GetConfigurationAsync(CancellationToken.None);
        _provider.RequestRefresh();
        await _provider.GetConfigurationAsync(CancellationToken.None);
        _server.Requests.ShouldBe(2);

        _clock.Advance(MinimumRefreshInterval);
        await _provider.GetConfigurationAsync(CancellationToken.None);
        _server.Requests.ShouldBe(3);
    }

    [Fact]
    public async Task GetConfigurationAsync_WithNothingCachedAndTheServerDown_Throws()
    {
        _server.Fail = true;

        await Should.ThrowAsync<HttpRequestException>(
            () => _provider.GetConfigurationAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetConfigurationAsync_ConcurrentColdCalls_DownloadOnce()
    {
        var calls = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => _provider.GetConfigurationAsync(CancellationToken.None)));

        var configurations = await Task.WhenAll(calls);

        configurations.ShouldAllBe(c => c.SigningKeys.Count == 1);
        _server.Requests.ShouldBe(1);
    }

    public void Dispose()
    {
        _provider.Dispose();
        _keyA.Dispose();
        _keyB.Dispose();
    }
}
