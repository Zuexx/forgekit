## MODIFIED Requirements

### Requirement: Better Auth Uses Durable Local Storage

The system SHALL use the configured relational provider for Better Auth persistence, and the API
and Better Auth SHALL be configured from a single provider selection so the two cannot disagree
about which database either one is actually using.

#### Scenario: Local Better Auth uses SQLite

- **WHEN** the starter kit runs with the default configuration
- **THEN** Better Auth data is stored in SQLite
- **AND** users, sessions, accounts, and verification records survive process restart

#### Scenario: Production Better Auth uses selected provider

- **WHEN** the provider is changed to `Postgres` or `SqlServer`
- **THEN** Better Auth uses the same provider family as the application DbContext

#### Scenario: The API and Better Auth share one SQLite database

- **WHEN** the starter kit runs with the default configuration
- **THEN** the API's Better Auth schema and the Next.js Better Auth instance read and write the
  same SQLite file, not two files that merely share a schema
- **AND** a row written by one process is visible to the other without a restart

#### Scenario: One setting selects the provider for both sides

- **WHEN** a fork changes the database provider
- **THEN** a single documented setting determines the provider for both the API and Better Auth
- **AND** no scenario requires setting the provider in two places to keep them in agreement

#### Scenario: Concurrent local writes do not fail under the default provider

- **WHEN** the API and Better Auth both write to the shared local SQLite file at the same time
- **THEN** neither write is lost or rejected because of the other holding a lock

#### Scenario: Sharing an instance does not merge the schemas

- **WHEN** the API and Better Auth share one database service instance
- **THEN** the application's tables and Better Auth's tables remain in two distinguishable
  schemas within it
- **AND** no requirement in this document is satisfied by merging them into one
