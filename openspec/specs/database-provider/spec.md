# database-provider Specification

## Purpose
Defines runtime database provider selection, durable Better Auth persistence, isolated provider migration histories, and test-only use of EF Core InMemory.
## Requirements
### Requirement: Configurable Database Provider
The system SHALL select the EF Core database provider from configuration.

#### Scenario: SQLite is the default provider
- **WHEN** no database provider is configured
- **THEN** the API uses SQLite
- **AND** the SQLite connection string points to a local durable database file

#### Scenario: PostgreSQL provider is selected
- **WHEN** `Database:Provider` is `Postgres`
- **THEN** the API configures EF Core with Npgsql
- **AND** the PostgreSQL connection string is required

#### Scenario: SQL Server provider is selected
- **WHEN** `Database:Provider` is `SqlServer`
- **THEN** the API configures EF Core with SQL Server
- **AND** the SQL Server connection string is required

#### Scenario: Unknown provider fails fast
- **WHEN** `Database:Provider` has an unsupported value
- **THEN** application startup fails with a clear configuration error

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

### Requirement: Provider-Specific Migrations
The system SHALL keep EF Core migrations in separate projects for each supported database provider.

#### Scenario: SQLite migrations are available for first run
- **WHEN** a developer selects the SQLite migration project after cloning
- **THEN** application and Better Auth schemas can be created without external services
- **AND** existing SQLite migration identifiers remain stable

#### Scenario: PostgreSQL migrations are available
- **WHEN** a deployment selects PostgreSQL
- **THEN** application and Better Auth PostgreSQL migrations are available from the PostgreSQL migration project
- **AND** the migration chain does not discover SQLite or SQL Server migrations

#### Scenario: SQL Server migrations are available
- **WHEN** a deployment selects SQL Server
- **THEN** application and Better Auth SQL Server migrations are available from the SQL Server migration project
- **AND** the migration chain does not discover SQLite or PostgreSQL migrations

#### Scenario: Migration discovery is validated without external databases
- **WHEN** CI validates provider migrations
- **THEN** it can discover each provider and DbContext migration chain without connecting to a database server

### Requirement: EF InMemory Is Test-Only
The system SHALL NOT use EF Core InMemory as a runtime database provider.

#### Scenario: Runtime configuration requests in-memory storage
- **WHEN** a runtime provider value attempts to select EF Core InMemory
- **THEN** the configuration is rejected or ignored in favor of supported relational providers

#### Scenario: Tests use in-memory provider selectively
- **WHEN** a focused unit test does not need relational behavior
- **THEN** EF Core InMemory MAY be used as a test-only provider

