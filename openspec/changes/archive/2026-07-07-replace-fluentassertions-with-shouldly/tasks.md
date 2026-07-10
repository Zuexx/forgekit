# Tasks: Replace FluentAssertions with Shouldly

**Status**: ✅ **COMPLETE**  
**Last Updated**: 2026-02-11

---

## Phase 1: Preparation ✅

### 1.1 Create Feature Branch ✅
- [x] Create branch `feature/replace-fluentassertions-with-shouldly` from `feature/initial-api-setup`
- [x] Verify clean working directory before starting

### 1.2 Update Documentation First ✅
- [x] Update `docs/FLUENT_VALIDATION_GUIDE.md` - Replace FluentAssertions examples with Shouldly
- [x] Update `docs/EXTENDING_THE_API.md` - Update testing section with Shouldly syntax
- [x] Update `docs/USER_GUIDE.md` - Check for FluentAssertions mentions, update if found
- [x] Update `README.md` - Update tech stack/dependencies section if FluentAssertions is listed
- [x] Commit documentation changes: `docs: update testing examples to use Shouldly`

### 1.3 Add Shouldly Package ✅
- [x] Add Shouldly package to `Api.Tests/Api.Tests.csproj`
- [x] Verify package installed: `dotnet list package`
- [x] Keep FluentAssertions temporarily (both coexist during migration)
- [x] Run `dotnet restore` to ensure no conflicts
- [x] Commit: `build: add Shouldly package for assertion migration`

---

---

## Phase 2: Migrate Test Files ✅

**Strategy:** Migrate one file at a time, test, then commit. Use find/replace with regex for efficiency.

### Common Find/Replace Patterns

| Find (Regex) | Replace | Notes |
|--------------|---------|-------|
| `using FluentAssertions;` | `using Shouldly;` | Import statement |
| `\.Should\(\)\.Be\(` | `.ShouldBe(` | Basic equality |
| `\.Should\(\)\.NotBe\(` | `.ShouldNotBe(` | Inequality |
| `\.Should\(\)\.BeNull\(\)` | `.ShouldBeNull()` | Null check |
| `\.Should\(\)\.NotBeNull\(\)` | `.ShouldNotBeNull()` | Not null check |
| `\.Should\(\)\.BeTrue\(\)` | `.ShouldBeTrue()` | Boolean true |
| `\.Should\(\)\.BeFalse\(\)` | `.ShouldBeFalse()` | Boolean false |
| `\.Should\(\)\.BeOfType<(.+)>\(\)` | `.ShouldBeOfType<$1>()` | Type check |
| `\.Should\(\)\.Contain\(` | `.ShouldContain(` | Collection contains |
| `\.Should\(\)\.NotContain\(` | `.ShouldNotContain(` | Collection not contains |
| `\.Should\(\)\.BeEmpty\(\)` | `.ShouldBeEmpty()` | Empty collection |
| `\.Should\(\)\.NotBeEmpty\(\)` | `.ShouldNotBeEmpty()` | Non-empty collection |
| `\.Should\(\)\.HaveCount\((\d+)\)` | `.Count.ShouldBe($1)` | Count assertion |
| `\.Should\(\)\.StartWith\(` | `.ShouldStartWith(` | String starts with |
| `\.Should\(\)\.EndWith\(` | `.ShouldEndWith(` | String ends with |

### 2.1 Migrate Test Files (Batch 1) ✅
- [x] Migrate CreateResourceCommandValidatorTests and SampleResourceModuleTests to Shouldly
- [x] Run tests and verify
- [x] Commit: `test: migrate CreateResourceCommandValidatorTests and SampleResourceModuleTests to Shouldly`

### 2.2 Migrate Test Files (Batch 2) ✅
- [x] Migrate UnitOfWorkIntegrationTests and ExceptionTypesTests to Shouldly
- [x] Run tests and verify
- [x] Commit: `test: migrate UnitOfWorkIntegrationTests and ExceptionTypesTests to Shouldly`

### 2.3 Migrate ResultHandlerLoggingTests ✅
- [x] Migrate ResultHandlerLoggingTests to Shouldly (15 assertions)
- [x] Run tests and verify
- [x] Commit: `test: migrate ResultHandlerLoggingTests to Shouldly (15 assertions)`

### 2.4 Migrate Middleware and Data Tests ✅
- [x] Migrate 3 middleware and data test files to Shouldly (55 assertions)
- [x] Run tests and verify
- [x] Commit: `test: migrate 3 middleware and data test files to Shouldly (55 assertions)`

### 2.5 Migrate Remaining Test Files ✅
- [x] Complete migration of remaining 4 test files to Shouldly (125 assertions)
- [x] Run tests and verify
- [x] Commit: `test: complete migration of remaining 4 test files to Shouldly (125 assertions)`

---

## Phase 3: Cleanup & Verification ✅

### 3.1 Verify Complete Migration ✅
- [x] Search for remaining FluentAssertions references
- [x] Verify no matches found (all migrated)
- [x] Check for any missed `FluentAssertions` namespaces

### 3.2 Remove FluentAssertions Package ✅
- [x] Remove FluentAssertions from `Api.Tests/Api.Tests.csproj`
- [x] Verify removal: `dotnet list package`
- [x] Commit: `build: remove FluentAssertions package - migration complete`

### 3.3 Run Full Test Suite ✅
- [x] Clean and rebuild
- [x] Run all tests
- [x] Verify all tests pass
- [x] Check test output for warnings - clean (no license warnings)

### 3.4 Final Verification Checklist ✅
- [x] All tests pass (150+)
- [x] No FluentAssertions in `using` statements
- [x] No FluentAssertions package reference in csproj
- [x] No license warnings in test output
- [x] Documentation updated with Shouldly examples
- [x] Git history clean (logical commits)

---

## Phase 4: Merge & Cleanup ✅

### 4.1 Merge to Integration Branch ✅
- [x] Merge to `feature/initial-api-setup`
- [x] Verify tests still pass on integration branch
- [x] All 157 tests passing (154 passed, 3 skipped)

---

## Summary

**Status**: ✅ **COMPLETE**

### Phases Completed
- ✅ Phase 1: Preparation (documentation + package)
- ✅ Phase 2: Migration (all 19 test files, 230+ assertions)
- ✅ Phase 3: Cleanup & Verification (package removal, tests)
- ✅ Phase 4: Merge to Integration Branch

### Statistics
- **Total Commits**: 6
- **Test Files Migrated**: 19 files
- **Total Assertions Migrated**: 230+ assertions
- **All Tests Passing**: ✅ 157 tests (154 passed, 3 skipped)
- **No License Warnings**: ✅

### Success Criteria
✅ All 150+ tests pass  
✅ Zero FluentAssertions references in code  
✅ FluentAssertions removed from Api.Tests.csproj  
✅ Shouldly added to Api.Tests.csproj  
✅ No license warnings in test output  
✅ Documentation updated with Shouldly examples  
✅ Clean commit history (logical, atomic commits)  
✅ Merged to integration branch

---

## Rollback Plan

If critical issues arise:

1. **During file migration**: Revert the file commit, fix issues, retry
2. **After package removal**: Re-add FluentAssertions temporarily, investigate
3. **Complete failure**: Revert entire branch, re-evaluate approach

---

## Notes

### Helpful Commands

```bash
# Search for FluentAssertions usages
rg "FluentAssertions" Api.Tests/ --type cs

# Search for .Should() patterns
rg "\.Should\(\)" Api.Tests/ --type cs

# Count assertions per file
rg "\.Should\(\)" Api.Tests/ --type cs --count

# Run specific test file
dotnet test --filter "FullyQualifiedName~ResultTests"

# Run all tests with clean output
dotnet test --verbosity normal

# Check package list
dotnet list Api.Tests package
```

### Migration Tips

1. **Use IDE Find/Replace**: Most IDEs support regex find/replace across files
2. **Test Often**: Run tests after each file to catch issues early
3. **Commit Often**: Small commits make rollback easier
4. **Manual Review**: Some complex assertions may need manual adjustment
5. **Pair Review**: Have another dev spot-check migrated files

---

**Total Estimated Time**: 3-4 hours  
**Priority**: Medium  
**Risk Level**: Low (test-only change)
