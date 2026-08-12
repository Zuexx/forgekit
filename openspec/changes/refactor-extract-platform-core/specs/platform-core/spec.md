# platform-core Specification

## ADDED Requirements

### Requirement: Product-Neutral Shared Layer

The system SHALL provide a shared API project whose directory name, assembly name, and root namespace are identical in the starter kit and in every project generated from it.

#### Scenario: Shared layer survives project generation

- **WHEN** a project is generated from the template with any project name
- **THEN** the shared project directory, assembly name, and root namespace SHALL be unchanged from the starter kit

#### Scenario: Shared layer declares no product types

- **WHEN** the shared project is compiled without a reference to the product project
- **THEN** the build SHALL succeed

### Requirement: Upstream Merge Compatibility

The system SHALL keep shared-layer file paths identical between the starter kit and generated projects so that upstream changes to the shared layer merge without path conflicts.

#### Scenario: Shared-layer change merges cleanly downstream

- **WHEN** a generated project merges an upstream commit that modifies only shared-layer files
- **THEN** the merge SHALL complete without path-rename conflicts

#### Scenario: Product-layer paths remain product-specific

- **WHEN** a project is generated from the template
- **THEN** the product project directory and namespace SHALL carry the product name

### Requirement: Database Model Stability Across the Split

The system SHALL keep the generated EF Core model unchanged when shared database configuration moves into the shared layer.

#### Scenario: No model drift after extraction

- **WHEN** a migration is generated for any supported provider after the shared layer is extracted
- **THEN** the migration SHALL contain no operations

#### Scenario: Existing migration identifiers are preserved

- **WHEN** the migration history is inspected after the shared layer is extracted
- **THEN** every existing migration identifier SHALL be unchanged
