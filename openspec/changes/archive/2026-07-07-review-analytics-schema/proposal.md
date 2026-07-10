# Change: Add Analytics Support for TODO Domain

## Why

The Scaffold API needs structured analytics to support workspace-level reporting and activity tracking. Two complementary analytics entities are required:

1. **WorkspaceAnalytics** — Period-based aggregated statistics (total todos, completions, overdue, cancelled, average completion days, active members) per workspace per reporting period. Supports dashboards and historical comparisons.

2. **DailyActivitySnapshot** — Daily granular snapshot of activity (todos created/completed/deleted, active members) per workspace. Enables contribution-graph-style visualisations.

## What Changes

- **NEW:** `WorkspaceAnalytics` entity in `Analytics/WorkspaceAnalytics`
- **NEW:** `DailyActivitySnapshot` entity in `Analytics/DailyActivitySnapshot`
- **NEW:** DbSet registrations in `AppDbContext`
- **NEW:** EF Core configurations and indexes for analytics queries
- **NEW:** Background service or scheduled task to populate snapshots
- **NEW:** Tests for analytics data consistency

## Impact

- **Affected specs:** analytics (new)
- **Affected code:**
  - `Api/Entities/Analytics/WorkspaceAnalytics.cs` (new)
  - `Api/Entities/Analytics/DailyActivitySnapshot.cs` (new)
  - `Api/Data/AppDbContext.cs` (new DbSets and configurations)
  - Migration required
- **Breaking changes:** None (additive only)
- **Database changes:** Two new tables

## Success Criteria

- ✅ WorkspaceAnalytics stores aggregated period statistics per workspace
- ✅ DailyActivitySnapshot stores daily granular activity per workspace
- ✅ Both entities inherit from BaseEntity (audit + soft-delete)
- ✅ Indexes optimised for date-range and workspace queries
- ✅ Tests verify entity creation and query patterns
