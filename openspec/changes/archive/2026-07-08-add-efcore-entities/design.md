# Design: EF Core Entity Foundations

## Context
The current API uses a workspace-based scaffold domain. The data model should support core workspace membership, configurable categories and labels, analytics rollups, audit metadata, and soft deletion without carrying domain-specific metadata from the original fork.

## Goals
- Provide a minimal, reusable EF Core model for scaffold applications.
- Keep ownership and membership generic: `Workspace` groups `Member` records.
- Support category and label configuration per workspace or globally.
- Store analytics data at workspace and daily activity levels.
- Apply consistent audit and soft delete behavior across entities.

## Non-Goals
- Domain-specific workflows from the original fork.
- EF Core migrations.
- A full task/todo service implementation.
- Read-optimized reporting projections beyond the analytics entities.

## Entity Overview

```
Workspace
  ├─ Members
  ├─ WorkspaceAnalytics
  └─ DailyActivitySnapshot

Category
  ├─ ParentCategory
  ├─ ChildCategories
  └─ CategoryLabels

Label
  └─ CategoryLabels
```

## Decisions

### Base Entity
All domain entities inherit from `BaseEntity`, which provides:
- `Id` as a 32-character lowercase GUID string.
- `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`.
- `Version` for optimistic version tracking.
- `IsDeleted`, `DeletedAt`, `DeletedBy` for soft delete.

### Workspace Membership
`Workspace` is the root grouping entity. `Member` links an auth user ID to a workspace and role. The model enforces a unique `(WorkspaceId, UserId)` membership.

### Category And Label Configuration
`Category` supports optional workspace scope and self-referencing hierarchy through `ParentCategoryId`. `Label` is workspace-scoped. `CategoryLabel` is the join entity between categories and labels.

### Analytics
`WorkspaceAnalytics` stores aggregate metrics for a workspace over a reporting period. `DailyActivitySnapshot` stores daily activity counts for a workspace. Extended analytics data can be stored in `WorkspaceAnalytics.MetricsJson`.

### DbContext Configuration
`AppDbContext` configures:
- Global soft delete query filters for all scaffold entities.
- Relationships for category hierarchy, category labels, workspace membership, and analytics records.
- Indexes for common workspace/category/analytics lookup patterns.
- JSON text mapping and decimal precision for analytics metrics.
- Camel-case relational object naming.
- Automatic audit timestamp updates in `SaveChanges` and `SaveChangesAsync`.

## Risks / Trade-offs
- JSON metrics are flexible but harder to query; promote frequently queried values to columns.
- Global query filters simplify normal reads but require explicit `IgnoreQueryFilters()` for restore/admin flows.
- Camel-case naming affects generated migrations; migration diffs must be reviewed.

## Migration Plan
1. Maintain entity classes and DbContext configuration in source.
2. Generate migrations separately when the schema is ready to deploy.
3. Review generated migrations for naming, indexes, precision, and query-filter assumptions.
