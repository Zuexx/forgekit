# Analytics Implementation Summary

## What Was Built

### WorkspaceAnalytics
Period-based aggregated statistics per workspace. Designed for dashboard KPI cards and historical reporting. Stores total todos, completions, overdue count, cancellations, average completion days, and active member count for a defined time window.

### DailyActivitySnapshot
Daily granular activity snapshot per workspace. Designed for contribution-graph-style visualisations (similar to GitHub's activity graph). Records todos created, completed, deleted, and active members per day.

## Architecture Decisions

Both entities inherit from `BaseEntity`, gaining:
- Soft-delete support with global query filters
- Full audit trail (CreatedBy, UpdatedAt, etc.)
- Optimistic concurrency (Version field)

## Query Performance

- `WorkspaceAnalytics`: indexed on `(WorkspaceId, PeriodStart, PeriodEnd)` — supports efficient period range queries
- `DailyActivitySnapshot`: indexed on `(WorkspaceId, SnapshotDate)` — supports 30/90-day contribution graph queries

## Extensibility

`WorkspaceAnalytics.MetricsJson` allows storing ad-hoc metrics without schema migration, enabling future analytics requirements to be met without breaking changes.
