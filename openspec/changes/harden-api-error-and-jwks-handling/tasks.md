## 1. Error responses

- [ ] 1.1 Unhandled exceptions return a generic message: an exception mapped to 500 yields the fixed generic `message`/`detail` while mapped exceptions keep their own, and the logged entry still carries the original exception. Done when a middleware test with a secret-bearing `InvalidOperationException` asserts the status, the generic text, and the secret's absence, and a test asserts the logger received the original exception.
- [ ] 1.2 Error bodies are camelCase: the middleware serializes with web defaults. Done when a test parses the raw body with `JsonDocument` and asserts `message`, `code`, `traceId`, `title`, `status`, `detail` exist and `Message`/`Code` do not.

## 2. Signing keys

- [ ] 2.1 `JwksProvider` caches the whole key set with a fixed refresh interval and a minimum refresh interval, parses with `JsonWebKeySet`, and implements `IConfigurationManager<OpenIdConnectConfiguration>` through `IJwksProvider`. Done when Anvil.Tests show: repeated unknown kids cause at most one extra download per minimum interval; a key removed from the published set is gone after refresh; a newly published key is found after the minimum interval; a failed refresh keeps the cached set.
- [ ] 2.2 JWT bearer authentication takes keys from the configuration manager instead of the blocking resolver. Done when an Anvil.Tests test authenticates a signed token through the real `JwtBearerHandler` and rejects one signed by an unpublished key, and the codebase contains no `GetAwaiter().GetResult()` on the key path.
- [ ] 2.3 Anvil no longer references `Newtonsoft.Json`. Done when `dotnet build` passes with the reference removed from `Anvil.csproj`.

## 3. Verification

- [ ] 3.1 `dotnet build` and `dotnet test` pass in `api/`, and `openspec validate harden-api-error-and-jwks-handling` passes.
