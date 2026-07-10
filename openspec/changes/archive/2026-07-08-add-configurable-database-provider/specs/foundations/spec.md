## ADDED Requirements

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
