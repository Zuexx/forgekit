# Analytics Gap Analysis: TODO Domain

## Summary

The Scaffold API's TODO domain requires analytics capabilities beyond real-time CRUD queries. This document compares requirements against the implemented analytics entities.

## Requirements vs Implementation

| Requirement | Status | Entity |
|---|---|---|
| Period-based aggregated stats (total, completed, overdue) | ✅ Implemented | `WorkspaceAnalytics` |
| Average completion time per period | ✅ Implemented | `WorkspaceAnalytics.AverageCompletionDays` |
| Active member count per period | ✅ Implemented | `WorkspaceAnalytics.ActiveMembers` |
| Daily activity for contribution graph | ✅ Implemented | `DailyActivitySnapshot` |
| Todos created/completed/deleted per day | ✅ Implemented | `DailyActivitySnapshot` |
| Per-workspace scoping | ✅ Implemented | Both entities have `WorkspaceId` |
| Historical trend queries (date range) | ✅ Implemented | Indexes on date fields |
| Extensible metrics | ✅ Implemented | `WorkspaceAnalytics.MetricsJson` |

## Gaps Identified

| Gap | Priority | Recommendation |
|---|---|---|
| Category-level breakdown | Medium | Add `CategoryAnalytics` entity or extend `MetricsJson` |
| Member-level productivity stats | Low | Derive from `TodoItem` on demand or add `MemberAnalytics` |
| Real-time KPI cards (live count) | Low | Use direct EF Core queries on `TodoItem`; snapshots for history |

## Conclusion

The two analytics entities (`WorkspaceAnalytics` + `DailyActivitySnapshot`) cover the primary reporting requirements for the TODO domain. Minor gaps can be addressed by extending `MetricsJson` or adding targeted entities in a future iteration.
