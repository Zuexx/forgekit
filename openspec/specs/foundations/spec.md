# foundations Specification

## Purpose
Defines starter-kit foundation data and setup expectations for local development and first-run validation.
## Requirements
### Requirement: Seed foundations for all entities
The system SHALL provide a seeder that creates at least five (5) records for each entity declared in AppDbContext.

#### Scenario: Seed creates records
- **WHEN** the seeder is executed against an empty development database with seeding enabled
- **THEN** the database SHALL contain at least five (5) records for each DbSet defined in AppDbContext and all required relationships SHALL be created.

#### Scenario: Seeding disabled via configuration
- **WHEN** the application runs with seeding disabled in configuration
- **THEN** the seeder SHALL NOT insert any sample data.

#### Scenario: Idempotent re-run
- **WHEN** the seeder is executed multiple times against the same database
- **THEN** no duplicate records SHALL be created and the operation SHALL complete without error.

### Requirement: Local Starter Database Defaults
The system SHALL provide foundation data and setup defaults that allow a fresh starter-kit clone to run locally without external infrastructure.

#### Scenario: Fresh local setup uses durable SQLite
- **WHEN** a developer clones the starter kit
- **AND** runs the documented setup commands
- **THEN** the API creates or uses a local SQLite database
- **AND** Better Auth data persists across process restarts

#### Scenario: External database is optional for first run
- **WHEN** PostgreSQL or SQL Server is unavailable locally
- **THEN** starter-kit smoke checks still run against SQLite
- **AND** no external database container is required
