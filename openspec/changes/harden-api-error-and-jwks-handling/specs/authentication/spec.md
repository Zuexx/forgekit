## ADDED Requirements

### Requirement: API Signing Keys Are Fetched Safely

The API SHALL validate bearer tokens against the signing keys published at the configured
JWKS URL, and SHALL obtain those keys so that a caller cannot use them to force outbound
requests, a key removed from the published set stops being trusted, and no request thread
blocks on the fetch.

**Acceptance Criteria:**
- The API MUST cache the published key set as a whole and replace it as a whole on each successful refresh
- The API MUST refresh the key set after a fixed refresh interval even when every token presents a known `kid`
- A token presenting an unknown `kid` MUST trigger at most one early refresh per minimum refresh interval, regardless of how many such tokens arrive
- Signing keys MUST be obtained asynchronously; the token validation path MUST NOT block on a pending fetch with `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`
- When a refresh fails and a key set is already cached, the API MUST keep using the cached set and retry no sooner than the minimum refresh interval
- The JWKS document MUST be parsed as a standard JSON Web Key Set (RFC 7517)

#### Scenario: A valid token is accepted
- **WHEN** a request carries a bearer token signed by a key in the published set, with the configured issuer and audience
- **THEN** the request is authenticated

#### Scenario: Tokens with random key ids cannot force downloads
- **WHEN** many tokens with distinct, unpublished `kid` values are presented within one minimum refresh interval
- **THEN** the JWKS document is downloaded at most once more than it was before they arrived
- **AND** each such token is rejected

#### Scenario: A rotated-out key stops being trusted
- **WHEN** the published set no longer contains a key that was previously cached
- **AND** the key set has been refreshed
- **THEN** a token signed by the removed key is rejected

#### Scenario: A newly published key is picked up
- **WHEN** the auth server publishes a new key after the set was cached
- **AND** a token signed by it arrives after the minimum refresh interval has elapsed
- **THEN** the key set is refreshed and the token's key is found
