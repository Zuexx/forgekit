# Tasks: Implement Soft-Delete Archiving Pattern

## Phase 1: Core Service Implementation (Effort: 2 hours)

- [x] Create `Api/Domain/Services/SoftDeleteDomainService.cs`
  - [x] Implement `MarkAsDeleted<T>()` method
  - [x] Implement `Restore<T>()` method
  - [x] Implement `CanRestore<T>()` method with 30-day grace period
  - [x] Add XML documentation

- [x] Verify global query filters in `AppDbContext`
  - [x] Confirm `HasQueryFilter(e => !e.IsDeleted)` exists for all deletable entities
  - [x] Document filter behavior

## Phase 2: Domain Service Integration (Effort: 3 hours)

- [x] Update sample application services to use `SoftDeleteDomainService`
  - [x] `TodoService.DeleteTodoAsync()` → use MarkAsDeleted()
  - [x] `TodoService.RestoreTodoAsync()` → use Restore() with eligibility check
  - [x] Add IgnoreQueryFilters() for restore queries
  - [x] Ensure IAuditContext is injected for userId

- [x] Update other domain services with soft-delete operations
  - [x] `SoftDeleteDomainService` injection into domain layer

- [x] Register `SoftDeleteDomainService` in DI container
  - [x] `Program.cs`: `builder.Services.AddScoped<SoftDeleteDomainService>()`

## Phase 3: Testing (Effort: 4 hours)

- [x] Create `Api.Tests/Domain/Services/SoftDeleteDomainServiceTests.cs`
  - [x] Test `MarkAsDeleted()` sets IsDeleted=true, DeletedAt, DeletedBy
  - [x] Test `Restore()` clears delete fields and updates audit fields
  - [x] Test `CanRestore()` returns true within 30 days
  - [x] Test `CanRestore()` returns false after 30 days (time-based test)
  - [x] Test `Restore()` throws exception when not eligible
  - [x] Test multiple restore cycles

- [x] Create `Api.Tests/Services/TodoServiceSoftDeleteTests.cs`
  - [x] Test `DeleteTodoAsync()` integration with domain service
  - [x] Test `RestoreTodoAsync()` with eligibility check
  - [x] Test query filters exclude soft-deleted records by default
  - [x] Test `IgnoreQueryFilters()` retrieves deleted records for restoration
  - [x] Test audit trail preservation (CreatedBy unchanged, UpdatedBy updated)

## Phase 4: Validation & Documentation (Effort: 2 hours)

- [x] Run all tests
  - [x] SoftDeleteDomainServiceTests: All passing
  - [x] TodoServiceSoftDeleteTests: All passing
  - [x] No regression in existing tests

- [x] Update openspec/specs/soft-delete/spec.md
  - [x] Add archiving pattern documentation
  - [x] Include restore grace period
  - [x] Document IgnoreQueryFilters() usage

- [x] Create ADR (Architecture Decision Record)
  - [x] `docs/adr/002-soft-delete-with-restore.md`
  - [x] Rationale for 30-day grace period
  - [x] Alternative patterns considered (event sourcing, archive tables)

## Success Criteria

- ✅ SoftDeleteDomainService created and tested
- ✅ Sample application-service deletion operations implemented
- ✅ Restore operations validate business rules
- ✅ Domain service and TodoService integration tests pass
- ✅ Query filters correctly exclude soft-deleted entities
- ✅ Audit trail preserved through delete/restore cycles
- ✅ Documentation updated

## Rollback Plan

If issues discovered:
1. Revert to previous commit
2. Review query filter configuration
3. Verify BaseEntity IsDeleted/DeletedAt/DeletedBy fields
4. Run focused test suite
5. Re-implement with adjustments

## Notes

- Grace period (30 days) is configurable via `MaxRestoreDays` constant in SoftDeleteDomainService
- IgnoreQueryFilters() should only be used in restore operations and admin interfaces
- Consider adding event sourcing in Phase 2 if audit trail becomes complex
- Future: Implement retention policies for automatic physical deletion after extended period
