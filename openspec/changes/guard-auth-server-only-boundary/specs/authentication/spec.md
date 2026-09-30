## ADDED Requirements

### Requirement: Server-Only Auth Modules Cannot Reach The Client Bundle

The system SHALL fail the production build if the auth configuration module or any database
adapter it composes is included in a client-side bundle, rather than allowing such a build to
succeed and fail silently at runtime.

#### Scenario: A build-time leak fails the build

- **WHEN** a client component is changed to import, directly or transitively, the auth
  configuration module or one of its database adapters
- **THEN** `pnpm build` SHALL fail with an error identifying the offending module
- **AND** no production build artifact SHALL be produced

#### Scenario: Existing server-only call sites are unaffected

- **WHEN** the auth configuration module is imported from a Route Handler, Server Component,
  or other server-only context
- **THEN** the build SHALL succeed and behavior SHALL be unchanged from before this guard
  existed
