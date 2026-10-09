## Context

`ExceptionHandlingMiddleware` and the JWKS code in `api/Anvil/` sit on every request and are
inherited verbatim by generated products. See the proposal for the three defects. The
middleware fix is local; the JWKS fix changes how the JWT bearer handler gets its keys.

## Goals / Non-Goals

**Goals:**
- 500 responses carry no server-side detail; the log keeps it.
- The error body's property names match `docs/api/API_ERRORS.md`, pinned by a test that reads
  raw JSON.
- Key fetches are bounded, the key set expires, and validation never blocks a thread.
- `Program.cs` (product layer) keeps compiling unchanged.

**Non-Goals:**
- Changing exception-to-status mapping, error codes, or titles.
- Requiring HTTPS for the JWKS URL, or moving its configuration key.
- Supporting key types other than those `JsonWebKeySet` already resolves.

## Decisions

### Generic 500 message is decided by the status, not the exception type

The middleware already computes the status code; when it is 500, `message` and `detail` are
a constant ("An unexpected error occurred."). Keying on the status rather than on a list of
"safe" exception types means a new unmapped exception is generic by default — the safe
failure direction. `title` is already generic ("Server Error").

### camelCase through a static `JsonSerializerOptions`

A `static readonly JsonSerializerOptions(JsonSerializerDefaults.Web)` in the middleware.
`Web` defaults give camelCase, matching ASP.NET Core's own response serialization.
Alternative considered: resolving `IOptions<JsonOptions>` from DI so the middleware follows
whatever the app configures — rejected, because then a product's serializer setting could
silently change a documented contract. Null-valued properties (`errors` on a 404) are kept
as `null`, as they are today; the docs' examples simply omit them.

### Keys reach the JWT bearer handler through `JwtBearerOptions.ConfigurationManager`

The bearer handler already knows how to get keys asynchronously: when
`Options.ConfigurationManager` is set it awaits `GetConfigurationAsync` before validating, uses
the returned `SigningKeys`, and calls `RequestRefresh()` when validation fails with
`SecurityTokenSignatureKeyNotFoundException`. Plugging into that removes the blocking
resolver entirely rather than hiding it.

`JwksProvider` therefore implements `IConfigurationManager<OpenIdConnectConfiguration>`
itself, returning a configuration whose only content is the parsed key set.
`IJwksProvider` extends that interface so `ConfigureJwtBearerOptions` can keep depending on
the abstraction it already injects.

Alternatives considered:
- *The library's `ConfigurationManager<T>` with a custom `IConfigurationRetriever`.* It
  provides the same intervals, but in IdentityModel 8.x a refresh with a cached configuration
  runs in the background and returns the stale set, which makes "a rotated-out key is
  rejected after refresh" non-deterministic to test, and its clock cannot be substituted.
  The policy needed here is about thirty lines; owning it buys a `TimeProvider` and
  deterministic tests.
- *Keep `IssuerSigningKeyResolver` and pre-warm the cache.* The resolver is synchronous by
  contract, so any cache miss still has to block or fail.

### Refresh policy

- Fixed refresh interval (default 1 hour): after it elapses, the next request refetches.
- Minimum refresh interval (default 30 seconds): `RequestRefresh()` is honored only if the
  last fetch attempt is at least this old. This is what bounds attacker-driven fetches.
- A fetch replaces the cached key set wholesale, so removed keys disappear.
- Fetches are serialized by a `SemaphoreSlim`; waiters re-check the cache after acquiring it,
  so concurrent requests produce one download.
- On fetch failure with a cached set: keep the cached set, schedule the next attempt one
  minimum interval later, log a warning. With no cached set: throw, as the old code did.

Both intervals are constructor parameters with defaults, alongside an optional `HttpClient`
and `TimeProvider`, so the existing `new JwksProvider(url)` call keeps working and tests can
drive time.

### Parsing with `JsonWebKeySet`

`new JsonWebKeySet(json).GetSigningKeys()` replaces the hand-rolled RSA-only parser and its
Newtonsoft dependency. It honours `use`/`alg` and supports the key types Better Auth can be
configured to emit, where the old parser silently ignored anything but RSA `n`/`e`.

## Risks / Trade-offs

- [`JsonWebKeySet` may not turn an EdDSA (Ed25519) JWK into a signing key, and EdDSA is
  Better Auth's default] → This kit pins `alg: "RS256"` in `app/lib/auth-instance.ts`, and the
  old parser was RSA-only too, so this is not a regression. The end-to-end test uses RS256. A
  product that switches Better Auth to EdDSA needs API-side support either way.
- [A newly rotated-in key is rejected for up to one minimum refresh interval if a token using
  it arrives just after a refresh] → 30 seconds; Better Auth publishes the new key alongside
  the old one during rotation, so in practice the set is already current.
- [Custom `IJwksProvider` implementations break] → Named as BREAKING in the proposal; the
  shared layer ships the only implementation.

## Migration Plan

No data migration. Products that sync `api/Anvil` get the new behavior with no product-layer
edit. Rollback is reverting the change.

## Open Questions

None blocking.
