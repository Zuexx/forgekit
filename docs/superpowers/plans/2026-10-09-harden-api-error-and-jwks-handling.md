# Harden API Error Responses and JWKS Key Handling Implementation Plan

**Goal:** 500 responses stop echoing exception messages, error bodies are camelCase as documented, and bearer-token signing keys are fetched asynchronously with a bounded, expiring cache.

**Spec:** `openspec/changes/harden-api-error-and-jwks-handling/` — `proposal.md`, `design.md`, `specs/`, `tasks.md`. This plan expands `tasks.md` into steps.

## OpenSpec Coverage

Change: `openspec/changes/harden-api-error-and-jwks-handling`

| Task ids | Plan task |
| --- | --- |
| 1.1 | Task 1 |
| 1.2 | Task 1 |
| 2.1 | Task 2 |
| 2.2 | Task 2 |
| 2.3 | Task 2 |
| 3.1 | Task 3 |

---

## Task 1: Error responses (`api/Anvil/Middlewares/ExceptionHandlingMiddleware.cs`)

- [x] Add a failing middleware test: an `InvalidOperationException` carrying a fake connection string yields 500, `INTERNAL_SERVER_ERROR`, the generic message in `message` and `detail`, and the secret absent from the raw body.
- [x] Add a failing test with a capturing logger: the logged entry is `Error`, holds the same exception instance, and its message contains the original text.
- [x] Add a failing test that parses the raw body with `JsonDocument` and asserts the exact camelCase property set.
- [x] Choose `message` by status code (500 → `GenericServerErrorMessage`), and serialize with a static `JsonSerializerOptions(JsonSerializerDefaults.Web)`.
- [x] Update the existing mapping theory: for 500 it now expects the generic message.
- [x] Update the 500 example in `docs/api/API_ERRORS.md`.

## Task 2: Signing keys (`api/Anvil/Foundations/JwksProvider.cs`, `Interfaces/IJwksProvider.cs`, `Extensions/ConfigureJwtBearerOptions.cs`)

- [x] `IJwksProvider` extends `IConfigurationManager<OpenIdConnectConfiguration>`; `GetKeyByIdAsync` kept.
- [x] Rewrite `JwksProvider`: whole-set cache, `JsonWebKeySet` parsing, refresh interval, minimum refresh interval honoured by `RequestRefresh`, `SemaphoreSlim`-serialized fetch, keep-last-good on failure, `TimeProvider` and `HttpClient` injectable, `new JwksProvider(url)` still valid, missing URL fails at fetch time rather than construction.
- [x] Anvil.Tests `JwksProviderTests`: cache hit, unknown-kid flood, unknown kid right after a fetch, rotation, new key after minimum interval, failure back-off, cold failure, concurrent cold calls.
- [x] `ConfigureJwtBearerOptions` sets `options.ConfigurationManager` and drops the blocking `IssuerSigningKeyResolver`.
- [x] Anvil.Tests `JwtBearerKeyResolutionTests` through the real `JwtBearerHandler`: published key accepted, unpublished key rejected with `SecurityTokenSignatureKeyNotFoundException`, repeated unknown keys cause no extra download. Mutation-checked: removing the `ConfigurationManager` assignment fails all three.
- [x] Remove `Newtonsoft.Json` from `Anvil.csproj`; update the `JwksProvider` entry in `docs/api/GLOSSARY.md`.

## Task 3: Verification

- [x] `dotnet build` in `api/`.
- [x] `dotnet test` in `api/`: Anvil.Tests all green; ForgeKit.Api.Tests green except the two Windows SQLite file-lock failures that predate this change (fixed separately on this branch).
- [x] `openspec validate harden-api-error-and-jwks-handling`.
