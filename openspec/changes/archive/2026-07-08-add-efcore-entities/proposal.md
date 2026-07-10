# Change: Add EF Core Entity Foundations

## Why
The API needs a small, consistent EF Core data model for workspace-based application features, configuration labels, analytics snapshots, audit fields, and soft delete behavior.

## What Changes
- Add base entity infrastructure with audit fields, optimistic versioning, and soft delete metadata.
- Add core workspace and member entities.
- Add category, label, and category-label configuration entities.
- Add workspace analytics and daily activity snapshot entities.
- Configure DbContext relationships, indexes, JSON/precision mappings, query filters, and automatic audit timestamp updates.
- Exclude migrations from this change; migrations are managed separately.

## Impact
- Affected specs: `data-model`
- Affected code: `Api/Entities/**`, `Api/Data/AppDbContext.cs`, EF model tests/docs
- Database: EF Core model ready for migration generation
- Breaking changes: none; this is foundational schema work
