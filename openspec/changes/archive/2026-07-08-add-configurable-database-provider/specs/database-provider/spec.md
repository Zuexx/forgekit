## ADDED Requirements

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
The system SHALL keep EF Core migrations separated by provider.

#### Scenario: SQLite migrations are available for first run
- **WHEN** a developer runs SQLite migration commands after cloning
- **THEN** application and Better Auth schemas can be created without external services

#### Scenario: PostgreSQL migrations remain available
- **WHEN** a deployment selects PostgreSQL
- **THEN** PostgreSQL-specific migrations are available without relying on SQLite migration output

#### Scenario: SQL Server migration strategy is explicit
- **WHEN** a deployment selects SQL Server
- **THEN** the project either provides SQL Server-specific migrations or documents the generation command before deployment

### Requirement: EF InMemory Is Test-Only
The system SHALL NOT use EF Core InMemory as a runtime database provider.

#### Scenario: Runtime configuration requests in-memory storage
- **WHEN** a runtime provider value attempts to select EF Core InMemory
- **THEN** the configuration is rejected or ignored in favor of supported relational providers

#### Scenario: Tests use in-memory provider selectively
- **WHEN** a focused unit test does not need relational behavior
- **THEN** EF Core InMemory MAY be used as a test-only provider
