# Analytics Design Rationale

## Why Two Entities?

The Scaffold API's analytics requirements span two distinct usage patterns with different granularity and lifetime needs. A single analytics entity cannot efficiently serve both.

---

## WorkspaceAnalytics — Period-Based Aggregation

### Use Case
Dashboard KPI cards, period comparisons (month-over-month), reporting exports.

### Design
One row per workspace per reporting period (e.g., monthly). Stores aggregated counts computed at period close or on demand.

```
WorkspaceId | PeriodStart | PeriodEnd  | TotalTodos | CompletedTodos | ...
------------+-------------+------------+------------+----------------+----
ws-abc123   | 2026-01-01  | 2026-01-31 | 24         | 18             | ...
ws-abc123   | 2026-02-01  | 2026-02-28 | 31         | 22             | ...
```

### Why This Approach
- **Aggregation at insert time** avoids expensive GROUP BY on large `TodoItem` tables at query time
- **Denormalized WorkspaceName** eliminates joins on every dashboard load
- **MetricsJson** allows adding custom KPIs (e.g., blocked todo count, priority breakdown) without schema changes
- Period boundaries are flexible — support weekly, monthly, or quarterly reporting windows

---

## DailyActivitySnapshot — Contribution Graph

### Use Case
Activity heatmap / contribution graph showing day-by-day intensity of todo activity in a workspace.

### Design
One row per workspace per calendar day. Captured end-of-day or incrementally throughout the day.

```
SnapshotDate | WorkspaceId | TodosCreated | TodosCompleted | ActiveMembers
-------------+-------------+--------------+----------------+--------------
2026-02-01   | ws-abc123   | 3            | 5              | 2
2026-02-02   | ws-abc123   | 1            | 2              | 1
2026-02-03   | ws-abc123   | 7            | 4              | 4
```

### Why This Approach
- **Daily granularity** is the natural unit for contribution graphs
- **Separate from WorkspaceAnalytics** because contribution graphs need individual day values (not period aggregates) and are queried as rolling 30/90-day windows rather than fixed periods
- **Low cardinality** — at most one row per workspace per day, making 1-year lookbacks cheap (365 rows vs scanning all TodoItem history)

---

## Summary

| Entity | Granularity | Query Pattern | Primary Use |
|---|---|---|---|
| `WorkspaceAnalytics` | Per period (week/month/quarter) | Fixed period boundaries | Dashboards, KPI cards, reports |
| `DailyActivitySnapshot` | Per day | Rolling window (last 30/90 days) | Contribution graph, activity feed |

Together, these two entities provide all analytics needed by the Scaffold API's reference TODO implementation without requiring expensive real-time aggregations on the transactional `TodoItem` table.
