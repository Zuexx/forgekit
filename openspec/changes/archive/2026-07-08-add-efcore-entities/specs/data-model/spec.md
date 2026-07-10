## ADDED Requirements

### Requirement: Base Entity Infrastructure
The system SHALL provide base entity infrastructure for IDs, audit timestamps, audit users, optimistic versioning, and soft delete metadata.

#### Scenario: Entity is created
- **WHEN** a new scaffold entity is saved
- **THEN** it has a 32-character lowercase GUID string ID
- **AND** create/update timestamps are populated

#### Scenario: Entity is soft deleted
- **WHEN** an entity is deleted through soft delete behavior
- **THEN** `IsDeleted`, `DeletedAt`, and `DeletedBy` preserve deletion metadata
- **AND** the record remains in the database

### Requirement: Workspace Membership
The system SHALL model workspaces and members so users can belong to workspaces with roles.

#### Scenario: User joins workspace
- **WHEN** a member is created for a workspace
- **THEN** the member references the workspace and auth user ID
- **AND** the `(WorkspaceId, UserId)` pair is unique

#### Scenario: Workspace loads members
- **WHEN** a workspace is queried with members included
- **THEN** its member collection represents users assigned to that workspace

### Requirement: Category And Label Configuration
The system SHALL support hierarchical categories and workspace labels connected through a join entity.

#### Scenario: Category hierarchy is configured
- **WHEN** a category has a parent category
- **THEN** the system preserves parent and child category navigation

#### Scenario: Category is labeled
- **WHEN** a category is associated with a label
- **THEN** the system stores the association in `CategoryLabel`
- **AND** category and label navigation properties resolve the relationship

### Requirement: Workspace Analytics
The system SHALL provide analytics entities for workspace-level reporting.

#### Scenario: Period analytics are stored
- **WHEN** reporting data is captured for a workspace period
- **THEN** `WorkspaceAnalytics` stores counts, active members, average completion days, and optional JSON metrics

#### Scenario: Daily activity is stored
- **WHEN** daily activity is captured for a workspace
- **THEN** `DailyActivitySnapshot` stores created, completed, deleted, and active member counts for that date

### Requirement: DbContext Model Configuration
The system SHALL configure EF Core relationships, indexes, query filters, and database naming conventions for scaffold entities.

#### Scenario: Normal queries exclude soft-deleted records
- **WHEN** a scaffold entity has `IsDeleted = true`
- **THEN** normal DbContext queries exclude it through global query filters

#### Scenario: Analytics queries use configured indexes
- **WHEN** analytics are queried by workspace and date
- **THEN** configured model indexes support workspace/date lookup patterns

#### Scenario: Relational objects use camel-case names
- **WHEN** EF Core builds the relational model
- **THEN** table, column, index, key, and foreign-key names use camel-case conventions
