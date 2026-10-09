using Anvil.Interfaces;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Anvil.Foundations
{
    /// <summary>
    /// Caches the signing keys published at a JWKS URL and hands them to the JWT bearer
    /// handler through <see cref="Microsoft.IdentityModel.Protocols.IConfigurationManager{T}"/>.
    /// </summary>
    /// <remarks>
    /// The key set is cached and replaced as a whole, so a key the auth server rotates out
    /// stops being trusted at the next refresh. A refresh happens when
    /// <c>refreshInterval</c> has elapsed, or early when the bearer handler meets an unknown
    /// <c>kid</c> and calls <see cref="RequestRefresh"/> — but never more than once per
    /// <c>minimumRefreshInterval</c>, so tokens carrying random kids cannot be used to force
    /// outbound requests.
    /// </remarks>
    public sealed class JwksProvider : IJwksProvider, IDisposable
    {
        public static readonly TimeSpan DefaultRefreshInterval = TimeSpan.FromHours(1);
        public static readonly TimeSpan DefaultMinimumRefreshInterval = TimeSpan.FromSeconds(30);

        private readonly string _jwksUrl;
        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;
        private readonly TimeProvider _timeProvider;
        private readonly TimeSpan _refreshInterval;
        private readonly TimeSpan _minimumRefreshInterval;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        // UTC ticks, read and written atomically: RequestRefresh runs outside the lock.
        private OpenIdConnectConfiguration? _current;
        private long _lastAttemptTicks;
        private long _refreshAfterTicks;

        public JwksProvider(
            string jwksUrl,
            HttpClient? httpClient = null,
            TimeProvider? timeProvider = null,
            TimeSpan? refreshInterval = null,
            TimeSpan? minimumRefreshInterval = null)
        {
            // Not validated here: the provider is built whenever bearer options are, including
            // in hosts that never authenticate a request, so a missing URL fails on first fetch.
            _jwksUrl = jwksUrl;
            _ownsHttpClient = httpClient is null;
            _httpClient = httpClient ?? new HttpClient();
            _timeProvider = timeProvider ?? TimeProvider.System;
            _refreshInterval = refreshInterval ?? DefaultRefreshInterval;
            _minimumRefreshInterval = minimumRefreshInterval ?? DefaultMinimumRefreshInterval;
        }

        public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel)
        {
            var current = Volatile.Read(ref _current);
            if (current is not null && UtcNowTicks() < Interlocked.Read(ref _refreshAfterTicks))
            {
                return current;
            }

            await _refreshLock.WaitAsync(cancel);
            try
            {
                // Another caller may have refreshed while this one waited for the lock.
                current = _current;
                var now = UtcNowTicks();
                if (current is not null && now < Interlocked.Read(ref _refreshAfterTicks))
                {
                    return current;
                }

                Interlocked.Exchange(ref _lastAttemptTicks, now);
                try
                {
                    if (string.IsNullOrWhiteSpace(_jwksUrl))
                    {
                        throw new InvalidOperationException(
                            "No JWKS URL is configured; set JwksCallBackUrl:Jwks.");
                    }

                    var json = await _httpClient.GetStringAsync(_jwksUrl, cancel);
                    var configuration = new OpenIdConnectConfiguration
                    {
                        JsonWebKeySet = new JsonWebKeySet(json)
                    };
                    foreach (var key in configuration.JsonWebKeySet.GetSigningKeys())
                    {
                        configuration.SigningKeys.Add(key);
                    }

                    Volatile.Write(ref _current, configuration);
                    Interlocked.Exchange(ref _refreshAfterTicks, now + _refreshInterval.Ticks);
                    return configuration;
                }
                catch (Exception) when (current is not null && !cancel.IsCancellationRequested)
                {
                    // Keep validating against the last good set rather than failing every
                    // request while the auth server is unreachable; try again shortly.
                    Interlocked.Exchange(ref _refreshAfterTicks, now + _minimumRefreshInterval.Ticks);
                    return current;
                }
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        public void RequestRefresh()
        {
            // Honoured once the minimum interval has passed since the last fetch; until then
            // the next fetch is simply scheduled for when it does, which a flood of requests
            // with unknown kids cannot bring forward.
            var earliest = Interlocked.Read(ref _lastAttemptTicks) + _minimumRefreshInterval.Ticks;
            var refreshAt = Math.Max(UtcNowTicks(), earliest);

            var scheduled = Interlocked.Read(ref _refreshAfterTicks);
            while (refreshAt < scheduled)
            {
                var observed = Interlocked.CompareExchange(ref _refreshAfterTicks, refreshAt, scheduled);
                if (observed == scheduled)
                {
                    break;
                }
                scheduled = observed;
            }
        }

        public async Task<SecurityKey?> GetKeyByIdAsync(string kid)
        {
            var configuration = await GetConfigurationAsync(CancellationToken.None);
            var key = FindKey(configuration, kid);
            if (key is not null)
            {
                return key;
            }

            RequestRefresh();
            configuration = await GetConfigurationAsync(CancellationToken.None);
            return FindKey(configuration, kid);
        }

        public void Dispose()
        {
            _refreshLock.Dispose();
            if (_ownsHttpClient)
            {
                _httpClient.Dispose();
            }
        }

        private long UtcNowTicks() => _timeProvider.GetUtcNow().UtcTicks;

        private static SecurityKey? FindKey(OpenIdConnectConfiguration configuration, string kid) =>
            configuration.SigningKeys.FirstOrDefault(k => string.Equals(k.KeyId, kid, StringComparison.Ordinal));
    }
}
