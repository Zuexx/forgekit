# ForgeKit API — EF Core Data Model

## Overview

This is the Entity Framework Core data model for the ForgeKit API — a TODO management reference implementation built with .NET 10 and ASP.NET Core. The model organises work items into Workspaces with Members, hierarchical Categories, Labels, and full audit/analytics support.

## Entity Structure

### Base

#### BaseEntity
Abstract base for all entities. Provides audit fields, soft-delete, and optimistic concurrency.

| Property | Type | Notes |
|---|---|---|
| `Id` | `string` | 32-char lowercase GUID |
| `CreatedAt` / `UpdatedAt` | `DateTime` | Auto-managed |
| `CreatedBy` / `UpdatedBy` | `string` | Set by application layer |
| `Version` | `int` | Optimistic concurrency |
| `IsDeleted` / `DeletedAt` / `DeletedBy` | — | Soft-delete support |

---

### Core

#### Workspace (`Core/Workspace`)
Top-level container grouping todos and members.

| Property | Type |
|---|---|
| `WorkspaceCode` | `string` (unique) |
| `WorkspaceName` | `string` |
| `Description` | `string?` |
| `IsActive` | `bool` |
| `OwnerId` | `string` |

**Relationships:** → `Member` (1:M), → `TodoItem` (1:M), → `Category` (1:M), → `Label` (1:M), → `WorkspaceAnalytics` (1:M), → `DailyActivitySnapshot` (1:M)

#### Member (`Core/Member`)
A user's membership in a Workspace.

| Property | Type |
|---|---|
| `WorkspaceId` | `string` (FK → Workspace) |
| `UserId` | `string` |
| `Role` | `string` |
| `JoinedAt` | `DateTime` |

---

### Configuration

#### Category (`Configuration/Category`)
Hierarchical category tree scoped to a Workspace.

| Property | Type |
|---|---|
| `CategoryCode` | `string` (unique within Workspace) |
| `CategoryName` | `string` |
| `Color` | `string?` |
| `ParentCategoryId` | `string?` (self-FK) |
| `DisplayOrder` | `int` |
| `IsActive` | `bool` |
| `Description` | `string?` |
| `WorkspaceId` | `string` (FK → Workspace) |

**Relationships:** → `CategoryLabel` (1:M), self-referencing hierarchy

#### Label (`Configuration/Label`)
Tag / label scoped to a Workspace.

| Property | Type |
|---|---|
| `LabelCode` | `string` (unique within Workspace) |
| `LabelName` | `string` |
| `Color` | `string?` |
| `IsActive` | `bool` |
| `WorkspaceId` | `string` (FK → Workspace) |

#### CategoryLabel (`Configuration/CategoryLabel`)
Many-to-many junction between Category and Label.

| Property | Type |
|---|---|
| `CategoryId` | `string` (FK → Category) |
| `LabelId` | `string` (FK → Label) |

---

### Todos

#### TodoItem (`Todos/TodoItem`)
The main work-item entity.

| Property | Type |
|---|---|
| `WorkspaceId` | `string` (FK → Workspace) |
| `AssignedToMemberId` | `string?` (FK → Member) |
| `CategoryId` | `string?` (FK → Category) |
| `Title` | `string` |
| `Description` | `string?` |
| `Priority` | `string` |
| `CurrentStatus` | `string` (Todo → InProgress → Done / Cancelled) |
| `DueDate` | `DateTime?` |
| `CompletedAt` | `DateTime?` |
| `MetadataJson` | `string?` |

**Status flow:** `Todo` → `InProgress` → `Done` or `Cancelled`

#### TodoStatusHistory (`Todos/TodoStatusHistory`)
Immutable audit trail of every status change on a `TodoItem`.

| Property | Type |
|---|---|
| `TodoItemId` | `string` (FK → TodoItem) |
| `Status` | `string` |
| `Timestamp` | `DateTime` |
| `ChangedBy` | `string` |
| `Notes` | `string?` |

---

### Analytics

#### WorkspaceAnalytics (`Analytics/WorkspaceAnalytics`)
Aggregated statistics per Workspace per reporting period.

| Property | Type |
|---|---|
| `WorkspaceId` | `string` (FK → Workspace) |
| `WorkspaceName` | `string` |
| `PeriodStart` / `PeriodEnd` | `DateTime` |
| `TotalTodos` | `int` |
| `CompletedTodos` | `int` |
| `OverdueTodos` | `int` |
| `CancelledTodos` | `int` |
| `AverageCompletionDays` | `decimal` |
| `ActiveMembers` | `int` |
| `MetricsJson` | `string?` |

#### DailyActivitySnapshot (`Analytics/DailyActivitySnapshot`)
Daily contribution-graph-style snapshot per Workspace.

| Property | Type |
|---|---|
| `SnapshotDate` | `DateTime` |
| `WorkspaceId` | `string` (FK → Workspace) |
| `TodosCreated` | `int` |
| `TodosCompleted` | `int` |
| `TodosDeleted` | `int` |
| `ActiveMembers` | `int` |

---

### Auth

Auth entities (`Auth/*`) are managed by Better Auth and stored in a separate `BetterAuthDbContext`. They are scaffolded database-first and are read-only from the application's perspective.

---

## Entity Relationships

```
Workspace
  ├─→ Member (1:M)
  ├─→ Category (1:M, hierarchical)
  │     └─→ CategoryLabel (1:M) ←── Label
  ├─→ TodoItem (1:M)
  │     ├─→ Category (M:1)
  │     └─→ TodoStatusHistory (1:M)
  ├─→ WorkspaceAnalytics (1:M)
  └─→ DailyActivitySnapshot (1:M)
```

### Diagram

```
┌──────────────┐       ┌──────────────┐
│  Workspace   │──────►│    Member    │
│              │──────►│  TodoItem    │──►TodoStatusHistory
│              │──────►│  Category    │
│              │       │  └─(self-FK) │
│              │──────►│   Label      │
│              │       │ CategoryLabel│ (Category × Label)
│              │──────►│WorkspaceAnalytics│
└──────────────┘──────►│DailyActivitySnapshot│
                        └──────────────┘
```

---

## AppDbContext

### Soft-Delete Global Filters

```csharp
modelBuilder.Entity<TodoItem>().HasQueryFilter(e => !e.IsDeleted);
modelBuilder.Entity<Workspace>().HasQueryFilter(e => !e.IsDeleted);
// ... applied to all BaseEntity subclasses
```

To include deleted records:

```csharp
var allItems = await context.TodoItems
    .IgnoreQueryFilters()
    .Where(t => t.WorkspaceId == workspaceId)
    .ToListAsync();
```

### Audit Tracking

`SaveChangesAsync(string userId)` on `AppDbContext` automatically sets `CreatedBy`, `UpdatedBy`, `CreatedAt`, `UpdatedAt` on all tracked entities.

### Usage Examples

```csharp
// Query todos for a workspace
var todos = await context.TodoItems
    .Include(t => t.Category)
    .Where(t => t.WorkspaceId == workspaceId && t.CurrentStatus == "Todo")
    .OrderBy(t => t.DueDate)
    .ToListAsync();

// Status history for a todo
var history = await context.TodoStatusHistory
    .Where(h => h.TodoItemId == todoId)
    .OrderBy(h => h.Timestamp)
    .ToListAsync();

// Workspace analytics for a period
var analytics = await context.WorkspaceAnalytics
    .Where(a => a.WorkspaceId == workspaceId
             && a.PeriodStart >= start
             && a.PeriodEnd <= end)
    .FirstOrDefaultAsync();

// Daily snapshots (contribution graph)
var snapshots = await context.DailyActivitySnapshots
    .Where(s => s.WorkspaceId == workspaceId
             && s.SnapshotDate >= DateTime.UtcNow.AddDays(-30))
    .OrderBy(s => s.SnapshotDate)
    .ToListAsync();
```

---

## ID Generation

All entities use 32-char lowercase GUIDs:

```csharp
Id = Guid.NewGuid().ToString("N").ToLower();
// e.g. "a1b2c3d4e5f6789012345678abcdef01"
```

---

## Migration Notes

User manages migrations separately:

```bash
# Install tools
dotnet tool install --global dotnet-ef

# Add migration
dotnet ef migrations add <MigrationName> --context AppDbContext

# Apply
dotnet ef database update --context AppDbContext
```

The `BetterAuthDbContext` is scaffolded database-first — do **not** include Auth tables in `AppDbContext` migrations.

---

## Testing

```csharp
[Fact]
public void TodoItem_StatusHistory_TrackChanges()
{
    var workspace = new Workspace { WorkspaceCode = "WS01", WorkspaceName = "My Workspace" };
    var todo = new TodoItem { WorkspaceId = workspace.Id, Title = "Write tests", CurrentStatus = "Todo" };
    var history = new TodoStatusHistory
    {
        TodoItemId = todo.Id,
        Status = "InProgress",
        Timestamp = DateTime.UtcNow,
        ChangedBy = "user-123"
    };

    Assert.Equal(todo.Id, history.TodoItemId);
    Assert.Equal("InProgress", history.Status);
}
```
