## Why

A code review found three defects in the shared layer's request-boundary code, each of which
a generated product inherits unchanged:

- An unhandled exception's own message is returned to the caller as `message` and `detail`,
  so a 500 can leak connection strings, SQL, file paths, or type names.
- Error responses are serialized with default `System.Text.Json` options and therefore emit
  PascalCase (`Message`, `Code`), while `docs/api/API_ERRORS.md` documents camelCase. The
  existing tests deserialize case-insensitively, so nothing caught it.
- `JwksProvider` downloads the JWKS document again for every token whose `kid` it has not
  seen, so anyone can force unbounded outbound requests with forged tokens. Its cache never
  expires, so a key the auth server has rotated out stays trusted for the life of the
  process. And the signing-key resolver blocks a request thread on that download with
  `.GetAwaiter().GetResult()`.

## What Changes

- Unhandled exceptions (anything mapped to 500) return a fixed generic `message`/`detail`.
  Mapped domain and validation exceptions keep their specific messages. The real exception
  is still logged with the correlation ID, which the response carries as `traceId`.
- **BREAKING (wire format):** error responses are serialized with camelCase property names,
  matching the documented contract. A client that read the undocumented PascalCase names
  must switch to the documented ones. No client in this repository reads them (checked with
  a literal search of `app/`).
- `JwksProvider` caches the whole key set, replaces it as a unit on refresh, refreshes it on
  a fixed interval, and refreshes early on an unknown `kid` no more often than a minimum
  interval. It parses the document with `Microsoft.IdentityModel.Tokens.JsonWebKeySet`.
- JWT bearer authentication obtains signing keys asynchronously through
  `JwtBearerOptions.ConfigurationManager`, replacing the blocking `IssuerSigningKeyResolver`.
- **BREAKING (shared-layer API):** `IJwksProvider` now extends
  `IConfigurationManager<OpenIdConnectConfiguration>`. `GetKeyByIdAsync` is kept. A product
  that implemented `IJwksProvider` itself must implement the two added members.
- Anvil drops its `Newtonsoft.Json` package reference; the JWKS parser was its only user.

### Not included

- No change to which exceptions map to which status codes, error codes, or titles.
- No change to the JWKS URL configuration key (`JwksCallBackUrl:Jwks`) or to
  `RequireHttpsMetadata`. Requiring HTTPS for the JWKS fetch is a separate decision.
- The product project's own `Newtonsoft.Json` reference is left alone: it is also reached
  transitively through `Microsoft.EntityFrameworkCore.Design`, so the direct reference may be
  acting as a version pin.
- The RFC 7807 `type`/`instance` fields the `exception-handling` spec lists are not added
  here; that gap predates this change.

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `error-response`: the ErrorResponse DTO is serialized with camelCase names; the middleware
  takes `message`/`detail` from the exception only for mapped exceptions and returns a
  generic message for 500s. The two requirements also stop naming the pre-Anvil file
  locations (`ForgeKit.Api/Models`, `ForgeKit.Api/Constants`), which no longer exist.
- `authentication`: adds a requirement on how the API obtains and refreshes the signing keys
  it validates bearer tokens against.

## Impact

CodeGraph is not indexed in this workspace, so the blast radius below comes from literal
searches (`grep` over `api/` and `app/`) rather than from `codegraph_explore`.

- `api/Anvil/Middlewares/ExceptionHandlingMiddleware.cs`: the only producer of
  `ErrorResponse`. Callers are the HTTP pipeline only (registered in `Program.cs`).
- `api/Anvil/Foundations/JwksProvider.cs`, `api/Anvil/Interfaces/IJwksProvider.cs`,
  `api/Anvil/Extensions/ConfigureJwtBearerOptions.cs`: the only users of the JWKS code. The
  product constructs `JwksProvider(string)` in `Program.cs`; that constructor is preserved,
  so the product layer needs no edit.
- String-addressed contracts checked by literal search: the JSON property names of the error
  body (no reader in `app/`; tests in `ForgeKit.Api.Tests/Middlewares` and
  `ForgeKit.Api.Tests/Integration/Middlewares` read them case-insensitively), the
  `Token-Expired` response header (unchanged), and the `JwksCallBackUrl:Jwks` config key
  (unchanged).
- What breaks if this is wrong: if the JWT bearer handler does not take keys from the
  configuration manager, every authenticated request is rejected with 401. An end-to-end test
  through the real `JwtBearerHandler` covers that path; nothing covered the old resolver.
- Dependencies: `Newtonsoft.Json` leaves `Anvil.csproj`. `Microsoft.IdentityModel.Protocols`
  and `.OpenIdConnect` are already present transitively through
  `Microsoft.AspNetCore.Authentication.JwtBearer`.
