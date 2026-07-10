# data-model Specification

## Purpose
Defines the EF Core ForgeKit data model: base entity infrastructure, workspaces, members, category/label configuration, sample todo entities, analytics snapshots, query filters, and database naming conventions.
## Requirements
### Requirement: Base Entity Infrastructure
The system SHALL provide base entity infrastructure for IDs, audit timestamps, audit users, optimistic versioning, and soft delete metadata.

#### Scenario: Entity is created
- **WHEN** a new ForgeKit entity is saved
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

### Requirement: Sample Todo Entities
The system SHALL provide TodoItem and TodoStatusHistory as a reference domain model for starter-kit application service conventions.

#### Scenario: Todo is created in a workspace
- **WHEN** a sample todo is created
- **THEN** it references a workspace
- **AND** it stores title, priority, current status, optional assignee, optional category, optional due date, and optional JSON metadata

#### Scenario: Todo status changes are recorded
- **WHEN** a todo status changes
- **THEN** a TodoStatusHistory record stores the todo ID, new status, timestamp, user ID, and optional notes

#### Scenario: Todo relationships resolve
- **WHEN** EF Core loads the sample todo model
- **THEN** TodoItem relates to Workspace, optional Member assignee, optional Category, and TodoStatusHistory records

### Requirement: Workspace Analytics
The system SHALL provide analytics entities for workspace-level reporting.

#### Scenario: Period analytics are stored
- **WHEN** reporting data is captured for a workspace period
- **THEN** `WorkspaceAnalytics` stores counts, active members, average completion days, and optional JSON metrics

#### Scenario: Daily activity is stored
- **WHEN** daily activity is captured for a workspace
- **THEN** `DailyActivitySnapshot` stores created, completed, deleted, and active member counts for that date

### Requirement: DbContext Model Configuration
The system SHALL configure EF Core relationships, indexes, query filters, and database naming conventions for ForgeKit entities.

#### Scenario: Normal queries exclude soft-deleted records
- **WHEN** a ForgeKit entity has `IsDeleted = true`
- **THEN** normal DbContext queries exclude it through global query filters

#### Scenario: Analytics queries use configured indexes
- **WHEN** analytics are queried by workspace and date
- **THEN** configured model indexes support workspace/date lookup patterns

#### Scenario: Relational objects use camel-case names
- **WHEN** EF Core builds the relational model
- **THEN** table, column, index, key, and foreign-key names use camel-case conventions

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
