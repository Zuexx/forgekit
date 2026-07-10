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
The system SHALL use the configured relational provider for Better Auth persistence.

#### Scenario: Local Better Auth uses SQLite
- **WHEN** the starter kit runs with the default configuration
- **THEN** Better Auth data is stored in SQLite
- **AND** users, sessions, accounts, and verification records survive process restart

#### Scenario: Production Better Auth uses selected provider
- **WHEN** the provider is changed to `Postgres` or `SqlServer`
- **THEN** Better Auth uses the same provider family as the application DbContext

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
