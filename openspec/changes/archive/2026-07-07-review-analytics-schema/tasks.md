# Tasks: Implement TODO Analytics Entities

## Phase 1: Entity Implementation

- [x] Create `Api/Entities/Analytics/WorkspaceAnalytics.cs`
  - [x] WorkspaceId (FK → Workspace)
  - [x] WorkspaceName (denormalized)
  - [x] PeriodStart / PeriodEnd (DateTime)
  - [x] TotalTodos, CompletedTodos, OverdueTodos, CancelledTodos (int)
  - [x] AverageCompletionDays (decimal)
  - [x] ActiveMembers (int)
  - [x] MetricsJson (string? for extensibility)

- [x] Create `Api/Entities/Analytics/DailyActivitySnapshot.cs`
  - [x] SnapshotDate (DateTime)
  - [x] WorkspaceId (FK → Workspace)
  - [x] TodosCreated, TodosCompleted, TodosDeleted (int)
  - [x] ActiveMembers (int)

## Phase 2: DbContext Configuration

- [x] Register DbSets in `AppDbContext`
- [x] Add global query filters (`!e.IsDeleted`)
- [x] Configure indexes:
  - `WorkspaceAnalytics`: `(WorkspaceId, PeriodStart, PeriodEnd)`
  - `DailyActivitySnapshot`: `(WorkspaceId, SnapshotDate)`
- [x] Add EF migration

## Phase 3: Data Population

- [x] Implement analytics aggregation logic in `TodoService`
- [x] Create daily snapshot background job or hook in `AppDbContext.SaveChangesAsync`
- [x] Implement period-based aggregation for WorkspaceAnalytics

## Phase 4: Testing

- [x] Unit tests for aggregation calculations
- [x] Integration tests for snapshot creation
- [x] Query performance tests for date-range filters

## Phase 5: Documentation

- [x] Update `Api/Entities/README.md` with analytics entity docs
- [x] Update `openspec/project.md` domain context

## Success Criteria

- ✅ Both entities implemented and migrated
- ✅ Indexes in place for analytics queries
- ✅ Tests passing
- ✅ Documentation updated
