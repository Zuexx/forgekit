## MODIFIED Requirements
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
