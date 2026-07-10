# Specification: TODO Analytics Requirements

**Status:** Implemented  
**Related Design:** ../../specs/data-model/design.md

---

## ADDED Requirements

### Requirement: WorkspaceAnalytics Entity

The system SHALL provide a `WorkspaceAnalytics` entity that aggregates statistics per workspace per reporting period.

**Acceptance Criteria:**
- Entity MUST be located in `Api/Entities/Analytics/WorkspaceAnalytics.cs`
- Entity MUST inherit from `BaseEntity`
- Entity MUST include: `WorkspaceId`, `WorkspaceName`, `PeriodStart`, `PeriodEnd`
- Entity MUST include counters: `TotalTodos`, `CompletedTodos`, `OverdueTodos`, `CancelledTodos`
- Entity MUST include `AverageCompletionDays` (decimal)
- Entity MUST include `ActiveMembers` (int)
- Entity MUST include `MetricsJson` (string?) for extensibility

#### Scenario: Query analytics for a workspace period
- **WHEN** a reporting query runs for WorkspaceId=X, PeriodStart=Jan, PeriodEnd=Feb
- **THEN** it returns a `WorkspaceAnalytics` row with aggregated counts for that period

#### Scenario: MetricsJson stores extended data
- **WHEN** additional metrics are needed beyond standard fields
- **THEN** they can be serialised into `MetricsJson` without schema migration

---

### Requirement: DailyActivitySnapshot Entity

The system SHALL provide a `DailyActivitySnapshot` entity capturing daily granular activity per workspace.

**Acceptance Criteria:**
- Entity MUST be located in `Api/Entities/Analytics/DailyActivitySnapshot.cs`
- Entity MUST inherit from `BaseEntity`
- Entity MUST include: `SnapshotDate`, `WorkspaceId`
- Entity MUST include: `TodosCreated`, `TodosCompleted`, `TodosDeleted`, `ActiveMembers`

#### Scenario: Contribution graph data
- **WHEN** a UI requests 30-day contribution data for a workspace
- **THEN** 30 `DailyActivitySnapshot` rows are returned, one per day
- **THEN** each row shows todos created/completed/deleted and active members for that day

#### Scenario: Snapshot uniqueness
- **WHEN** a snapshot is created for WorkspaceId=X on date D
- **THEN** only one snapshot SHALL exist for that workspace+date combination

---

### Requirement: Analytics Indexes

The system SHALL configure database indexes to support efficient analytics queries.

**Acceptance Criteria:**
- `WorkspaceAnalytics` MUST have a composite index on `(WorkspaceId, PeriodStart, PeriodEnd)`
- `DailyActivitySnapshot` MUST have a composite index on `(WorkspaceId, SnapshotDate)`
- Both entities MUST have soft-delete global query filters

#### Scenario: Analytics queries use indexed fields
- **WHEN** analytics are queried by workspace and reporting period
- **THEN** the configured composite indexes support efficient lookup by workspace and date range

---

## Testing Requirements

### Unit Tests
- Verify entity property assignments
- Verify index configuration via EF model

### Integration Tests
- Verify analytics queries with date-range filters perform within 500ms for 1-year datasets
- Verify soft-delete filters exclude deleted analytics records

---

## Acceptance Criteria Summary

- ✅ WorkspaceAnalytics entity with all required properties
- ✅ DailyActivitySnapshot entity with all required properties
- ✅ Both entities indexed for analytics queries
- ✅ Both entities covered by soft-delete global filters
- ✅ Tests passing
