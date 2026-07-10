## ADDED Requirements

### Requirement: Provider-Compatible EF Model
The EF Core model SHALL avoid provider-specific assumptions unless those assumptions are isolated in provider-specific configuration or migrations.

#### Scenario: SQLite model builds
- **WHEN** EF Core builds the application model for SQLite
- **THEN** the model includes workspace, member, category, label, todo, analytics, audit, and soft-delete entities
- **AND** model building does not require PostgreSQL-specific APIs

#### Scenario: PostgreSQL model builds
- **WHEN** EF Core builds the application model for PostgreSQL
- **THEN** the model uses PostgreSQL provider capabilities only through PostgreSQL-specific configuration or migrations

#### Scenario: SQL Server model builds
- **WHEN** EF Core builds the application model for SQL Server
- **THEN** the model uses SQL Server provider capabilities only through SQL Server-specific configuration or migrations
