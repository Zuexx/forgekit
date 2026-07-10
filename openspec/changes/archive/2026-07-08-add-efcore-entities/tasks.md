## 1. Entity Infrastructure
- [x] 1.1 Create `BaseEntity` with ID, audit fields, version, and soft delete metadata
- [x] 1.2 Create `IAuditableEntity`
- [x] 1.3 Create `ISoftDelete`

## 2. Core Entities
- [x] 2.1 Create `Workspace`
- [x] 2.2 Create `Member`
- [x] 2.3 Configure `Workspace` to `Member` relationship
- [x] 2.4 Add unique workspace/member indexes

## 3. Configuration Entities
- [x] 3.1 Create `Category`
- [x] 3.2 Create `Label`
- [x] 3.3 Create `CategoryLabel` join entity
- [x] 3.4 Configure category hierarchy
- [x] 3.5 Configure category-label relationships

## 4. Analytics Entities
- [x] 4.1 Create `WorkspaceAnalytics`
- [x] 4.2 Create `DailyActivitySnapshot`
- [x] 4.3 Configure analytics relationships to `Workspace`
- [x] 4.4 Configure analytics indexes
- [x] 4.5 Configure metrics JSON and decimal precision mappings

## 5. DbContext Configuration
- [x] 5.1 Add DbSet properties for scaffold entities
- [x] 5.2 Configure soft delete global query filters
- [x] 5.3 Configure entity relationships with Fluent API
- [x] 5.4 Configure indexes for common lookup patterns
- [x] 5.5 Configure JSON column mappings
- [x] 5.6 Configure camel-case relational object naming
- [x] 5.7 Update audit timestamps in `SaveChanges`
- [x] 5.8 Update audit timestamps in `SaveChangesAsync`

## 6. Documentation
- [x] 6.1 Add XML documentation comments to entity classes and properties
- [x] 6.2 Document entity model usage in `Api/Entities/README.md`
- [x] 6.3 Document indexes and soft delete behavior

## 7. Validation
- [x] 7.1 Add focused tests for entity relationship configuration
- [x] 7.2 Add focused tests for soft delete query filters on scaffold entities
- [x] 7.3 Add focused tests for audit timestamp updates in `AppDbContext`
- [x] 7.4 Add focused tests for analytics indexes/model configuration
