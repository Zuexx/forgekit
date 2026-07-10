## ADDED Requirements

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
