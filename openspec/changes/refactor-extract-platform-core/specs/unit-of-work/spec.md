# unit-of-work Specification

## ADDED Requirements

### Requirement: Product-Neutral DbContext Access

The system SHALL expose DbContext access through IUnitOfWork without naming a product-specific DbContext type, so that the Unit of Work can reside in the shared layer.

#### Scenario: Interface compiles without product types

- **WHEN** the shared project containing IUnitOfWork is compiled without a reference to the product project
- **THEN** the build SHALL succeed

#### Scenario: Transaction behavior is unchanged

- **WHEN** a service begins a transaction, saves changes with a user id, and commits
- **THEN** the audit fields SHALL be populated exactly as before the extraction
- **AND** the transaction SHALL commit atomically

#### Scenario: Product services reach product entity sets

- **WHEN** a product service accesses an entity set through IUnitOfWork
- **THEN** the entity set SHALL be reachable without casting to a product-specific DbContext type
