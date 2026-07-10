# Data Model Design: TODO Analytics Entities

## Entities

### WorkspaceAnalytics

```csharp
// Api/Entities/Analytics/WorkspaceAnalytics.cs
public class WorkspaceAnalytics : BaseEntity
{
    public string WorkspaceId { get; set; } = default!;
    public string WorkspaceName { get; set; } = default!;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalTodos { get; set; }
    public int CompletedTodos { get; set; }
    public int OverdueTodos { get; set; }
    public int CancelledTodos { get; set; }
    public decimal AverageCompletionDays { get; set; }
    public int ActiveMembers { get; set; }
    public string? MetricsJson { get; set; }

    // Navigation
    public Workspace Workspace { get; set; } = default!;
}
```

**Purpose:** Period-based aggregated statistics. One row per workspace per reporting period (e.g., monthly, quarterly). Used for dashboards, reporting, and historical trend analysis.

**Key Indexes:**
- `(WorkspaceId, PeriodStart, PeriodEnd)` — supports period queries

---

### DailyActivitySnapshot

```csharp
// Api/Entities/Analytics/DailyActivitySnapshot.cs
public class DailyActivitySnapshot : BaseEntity
{
    public DateTime SnapshotDate { get; set; }
    public string WorkspaceId { get; set; } = default!;
    public int TodosCreated { get; set; }
    public int TodosCompleted { get; set; }
    public int TodosDeleted { get; set; }
    public int ActiveMembers { get; set; }

    // Navigation
    public Workspace Workspace { get; set; } = default!;
}
```

**Purpose:** Daily granular activity snapshot. One row per workspace per day. Used for contribution-graph-style visualisations showing activity intensity over time.

**Key Indexes:**
- `(WorkspaceId, SnapshotDate)` — supports date-range queries

---

## EF Core Configuration

```csharp
// In AppDbContext.OnModelCreating()
modelBuilder.Entity<WorkspaceAnalytics>(entity =>
{
    entity.HasQueryFilter(e => !e.IsDeleted);
    entity.HasIndex(e => new { e.WorkspaceId, e.PeriodStart, e.PeriodEnd });
    entity.HasOne(e => e.Workspace)
          .WithMany(w => w.WorkspaceAnalytics)
          .HasForeignKey(e => e.WorkspaceId)
          .OnDelete(DeleteBehavior.Cascade);
});

modelBuilder.Entity<DailyActivitySnapshot>(entity =>
{
    entity.HasQueryFilter(e => !e.IsDeleted);
    entity.HasIndex(e => new { e.WorkspaceId, e.SnapshotDate });
    entity.HasOne(e => e.Workspace)
          .WithMany(w => w.DailyActivitySnapshots)
          .HasForeignKey(e => e.WorkspaceId)
          .OnDelete(DeleteBehavior.Cascade);
});
```

---

## Query Patterns

### Period analytics for a workspace

```csharp
var analytics = await context.WorkspaceAnalytics
    .Where(a => a.WorkspaceId == workspaceId
             && a.PeriodStart >= startDate
             && a.PeriodEnd <= endDate)
    .OrderBy(a => a.PeriodStart)
    .ToListAsync();
```

### 30-day contribution graph

```csharp
var snapshots = await context.DailyActivitySnapshots
    .Where(s => s.WorkspaceId == workspaceId
             && s.SnapshotDate >= DateTime.UtcNow.AddDays(-30))
    .OrderBy(s => s.SnapshotDate)
    .ToListAsync();
```

---

## Design Decisions

| Decision | Rationale |
|---|---|
| Separate `WorkspaceAnalytics` and `DailyActivitySnapshot` | Different granularity for different use cases — period reports vs contribution graph |
| Denormalize `WorkspaceName` in `WorkspaceAnalytics` | Avoid join cost on every analytics query; workspace names rarely change |
| `MetricsJson` on `WorkspaceAnalytics` | Extensibility without schema migration for ad-hoc metrics |
| Cascade delete | Analytics rows are meaningless without their parent workspace |
| Inherit `BaseEntity` | Consistent soft-delete and audit trail with the rest of the system |
