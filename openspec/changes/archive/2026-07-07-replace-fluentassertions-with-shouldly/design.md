# Design: Replace FluentAssertions with Shouldly

## Overview

This document outlines the technical approach for migrating from FluentAssertions to Shouldly across the test suite, including syntax mappings, migration patterns, and quality gates.

---

## Background

### FluentAssertions License Change

As of 2024, FluentAssertions transitioned from Apache 2.0 (open-source) to a **commercial license** under Xceed:
- Free for **non-commercial** use only
- **Commercial use requires paid subscription**
- License warnings appear in test output

**Warning message example:**
```
Warning:
The component "Fluent Assertions" is governed by the rules defined in the Xceed License Agreement
and the Xceed Fluent Assertions Community License. You may use Fluent Assertions free of charge
for non-commercial use only. An active subscription is required to use Fluent Assertions for
commercial use...
```

### Why This Matters

1. **Legal Compliance**: Corporate/commercial use now requires licensing
2. **Test Output Pollution**: Warnings clutter CI/CD logs
3. **Open-Source Principles**: Prefer truly open-source dependencies (MIT)
4. **Sustainability**: Avoid vendor lock-in for core testing infrastructure

---

## Shouldly as Replacement

### Selection Criteria

| Criterion | FluentAssertions | Shouldly | Winner |
|-----------|------------------|----------|---------|
| **License** | Commercial (Xceed) | MIT | ✅ Shouldly |
| **API Similarity** | Fluent `.Should().Be()` | Fluent `.ShouldBe()` | Tie (both easy) |
| **Error Messages** | Good | Excellent (context-aware) | ✅ Shouldly |
| **Maintenance** | Active | Active (7k+ stars) | Tie |
| **.NET 10 Support** | Yes | Yes | Tie |
| **Breaking Changes** | None (migration only) | None (migration only) | Tie |
| **Community** | Large | Large | Tie |
| **No Warnings** | ❌ Warnings | ✅ Clean output | ✅ Shouldly |

**Decision**: Shouldly wins on license, error messages, and clean output.

### Shouldly Benefits

1. **Better Error Messages**: Shouldly shows more context in failures
   ```csharp
   // FluentAssertions failure
   Expected value to be "expected", but found "actual".
   
   // Shouldly failure (more helpful!)
   result.Data
       should be
   "expected"
       but was
   "actual"
   ```

2. **Natural Syntax**: Reads like English
   ```csharp
   user.Name.ShouldBe("John");  // "user.Name should be 'John'"
   ```

3. **No License Warnings**: Clean test output, no commercial restrictions

---

## Syntax Migration Guide

### Common Patterns

#### Basic Equality

```csharp
// FluentAssertions
result.Should().Be(expected);
result.Should().NotBe(unexpected);

// Shouldly
result.ShouldBe(expected);
result.ShouldNotBe(unexpected);
```

#### Null Checks

```csharp
// FluentAssertions
obj.Should().BeNull();
obj.Should().NotBeNull();

// Shouldly
obj.ShouldBeNull();
obj.ShouldNotBeNull();
```

#### Type Checks

```csharp
// FluentAssertions
result.Should().BeOfType<Success>();
result.Should().BeAssignableTo<IResult>();

// Shouldly
result.ShouldBeOfType<Success>();
result.ShouldBeAssignableTo<IResult>();
```

#### Boolean Assertions

```csharp
// FluentAssertions
condition.Should().BeTrue();
condition.Should().BeFalse();

// Shouldly
condition.ShouldBeTrue();
condition.ShouldBeFalse();
```

#### Collection Assertions

```csharp
// FluentAssertions
list.Should().Contain(item);
list.Should().NotContain(item);
list.Should().BeEmpty();
list.Should().NotBeEmpty();
list.Should().HaveCount(3);

// Shouldly
list.ShouldContain(item);
list.ShouldNotContain(item);
list.ShouldBeEmpty();
list.ShouldNotBeEmpty();
list.Count.ShouldBe(3);  // Note: different pattern for count
```

#### String Assertions

```csharp
// FluentAssertions
str.Should().StartWith("prefix");
str.Should().EndWith("suffix");
str.Should().Contain("substring");

// Shouldly
str.ShouldStartWith("prefix");
str.ShouldEndWith("suffix");
str.ShouldContain("substring");
```

#### Exception Assertions

```csharp
// FluentAssertions
Action act = () => method();
act.Should().Throw<InvalidOperationException>();
act.Should().Throw<InvalidOperationException>()
    .WithMessage("Error message");

// Shouldly
Should.Throw<InvalidOperationException>(() => method());
Should.Throw<InvalidOperationException>(() => method())
    .Message.ShouldBe("Error message");
```

#### Complex Object Assertions

```csharp
// FluentAssertions
user.Should().BeEquivalentTo(expected);

// Shouldly (use multiple assertions)
user.Id.ShouldBe(expected.Id);
user.Name.ShouldBe(expected.Name);
user.Email.ShouldBe(expected.Email);
```

---

## Migration Strategy

### Phase 1: Documentation First

Update documentation **before** code to set expectations:

1. **docs/FLUENT_VALIDATION_GUIDE.md** - Change testing examples
2. **docs/EXTENDING_THE_API.md** - Update testing section
3. **docs/USER_GUIDE.md** - Update testing references
4. **README.md** - Update tech stack (if FluentAssertions listed)

**Benefit**: Developers see new syntax before they encounter it in code.

### Phase 2: Coexistence Period

Both libraries temporarily coexist:
- Add Shouldly package
- Keep FluentAssertions
- Migrate files one-by-one
- Remove FluentAssertions only when migration complete

**Benefit**: Allows gradual migration, easy rollback if issues arise.

### Phase 3: File-by-File Migration

For each test file:
1. Replace `using FluentAssertions;` → `using Shouldly;`
2. Apply regex find/replace patterns
3. Manual review for complex assertions
4. Run tests for that file
5. Commit when green

**Benefit**: Atomic commits, easy to revert specific files.

### Phase 4: Verification & Cleanup

1. Search for remaining FluentAssertions references
2. Remove FluentAssertions package
3. Run full test suite
4. Verify no warnings in output

**Benefit**: Ensures complete migration, clean final state.

---

## File Migration Order

Migrate in **ascending order of complexity** (simplest first):

| Priority | File | Usages | Complexity | Rationale |
|----------|------|--------|------------|-----------|
| 12 | CreateResourceCommandValidatorTests.cs | 1 | Low | Easiest, quick win |
| 11 | SampleResourceModuleTests.cs | 10 | Low | Simple integration tests |
| 10 | UnitOfWorkIntegrationTests.cs | 11 | Low | Straightforward DB tests |
| 9 | ExceptionTypesTests.cs | 14 | Low | Simple exception checks |
| 8 | ResultHandlerLoggingTests.cs | 15 | Medium | Logging assertions |
| 7 | ExceptionHandlingIntegrationTests.cs | 17 | Medium | Integration tests |
| 6 | CorrelationIdMiddlewareTests.cs | 19 | Medium | Middleware tests |
| 5 | UnitOfWorkTests.cs | 19 | Medium | Unit tests |
| 4 | ResultHandlerTests.cs | 24 | Medium | Handler tests |
| 3 | ExceptionHandlingMiddlewareTests.cs | 24 | Medium | Middleware tests |
| 2 | SampleHandlerTests.cs | 33 | High | Complex handler tests |
| 1 | ResultTests.cs | 44 | High | Most complex (core result type tests) |

**Strategy**: Start with easy files for quick wins, save complex files for last when patterns are established.

---

## Regex Find/Replace Patterns

### Primary Patterns (Cover ~80% of usages)

| Pattern ID | Find (Regex) | Replace | Count |
|------------|--------------|---------|-------|
| P1 | `using FluentAssertions;` | `using Shouldly;` | 12 |
| P2 | `\.Should\(\)\.Be\(` | `.ShouldBe(` | ~80 |
| P3 | `\.Should\(\)\.BeOfType<(.+?)>\(\)` | `.ShouldBeOfType<$1>()` | ~40 |
| P4 | `\.Should\(\)\.NotBeNull\(\)` | `.ShouldNotBeNull()` | ~30 |
| P5 | `\.Should\(\)\.BeTrue\(\)` | `.ShouldBeTrue()` | ~15 |
| P6 | `\.Should\(\)\.BeFalse\(\)` | `.ShouldBeFalse()` | ~15 |
| P7 | `\.Should\(\)\.Contain\(` | `.ShouldContain(` | ~15 |
| P8 | `\.Should\(\)\.BeEmpty\(\)` | `.ShouldBeEmpty()` | ~10 |

### Secondary Patterns (Cover remaining ~20%)

| Pattern ID | Find (Regex) | Replace | Notes |
|------------|--------------|---------|-------|
| S1 | `\.Should\(\)\.NotBe\(` | `.ShouldNotBe(` | Inequality |
| S2 | `\.Should\(\)\.BeNull\(\)` | `.ShouldBeNull()` | Null check |
| S3 | `\.Should\(\)\.NotContain\(` | `.ShouldNotContain(` | Collection |
| S4 | `\.Should\(\)\.NotBeEmpty\(\)` | `.ShouldNotBeEmpty()` | Collection |
| S5 | `\.Should\(\)\.HaveCount\((\d+)\)` | `.Count.ShouldBe($1)` | Count (different pattern!) |
| S6 | `\.Should\(\)\.StartWith\(` | `.ShouldStartWith(` | String |
| S7 | `\.Should\(\)\.EndWith\(` | `.ShouldEndWith(` | String |

### Exception Patterns (Manual Review Required)

```csharp
// FluentAssertions
act.Should().Throw<TException>();
act.Should().NotThrow();
act.Should().Throw<TException>().WithMessage("...");

// Shouldly (different structure!)
Should.Throw<TException>(() => method());
Should.NotThrow(() => method());
Should.Throw<TException>(() => method()).Message.ShouldBe("...");
```

**Note**: Exception assertions require **manual migration** - regex not sufficient.

---

## Quality Gates

### Per-File Gates

After migrating each file:
- [ ] No compile errors
- [ ] All tests in file pass
- [ ] No remaining `.Should()` calls in file
- [ ] No `using FluentAssertions;` in file

### Phase Gates

After completing each phase:
- [ ] All tests pass (dotnet test)
- [ ] No new warnings introduced
- [ ] Git history clean (logical commits)

### Final Gates

Before merging:
- [ ] All 150+ tests pass
- [ ] Zero FluentAssertions references (`rg "FluentAssertions" returns nothing`)
- [ ] FluentAssertions removed from csproj
- [ ] No license warnings in test output
- [ ] Documentation updated
- [ ] CI/CD pipeline green

---

## Risk Mitigation

### Risk 1: Tests Fail After Migration

**Probability**: Medium  
**Impact**: Medium  
**Mitigation**:
- Migrate one file at a time
- Run tests after each file
- Commit when green
- Easy to revert individual files

### Risk 2: Missed Assertions

**Probability**: Low  
**Impact**: Low  
**Mitigation**:
- Use regex search to find remaining patterns
- Search for `using FluentAssertions`
- Search for `.Should()`
- Peer review migrated files

### Risk 3: Complex Assertions Break

**Probability**: Low  
**Impact**: Medium  
**Mitigation**:
- Manually review exception assertions
- Manually review `BeEquivalentTo` (no direct Shouldly equivalent)
- Test thoroughly after migration

### Risk 4: Documentation Outdated

**Probability**: Low  
**Impact**: Low  
**Mitigation**:
- Update docs FIRST (Phase 1)
- Grep for "FluentAssertions" in docs
- Peer review doc changes

---

## Testing Strategy

### Unit Test Verification

For each migrated file:
```bash
# Run specific test file
dotnet test --filter "FullyQualifiedName~ResultTests"

# Verify output
# ✅ All tests pass
# ✅ No warnings
# ✅ Clean output
```

### Integration Test Verification

After all files migrated:
```bash
# Full test suite
dotnet test --verbosity normal

# Expected result:
# Test summary: total: 150+, failed: 0, succeeded: 150+
```

### Output Verification

```bash
# Capture test output
dotnet test > test-output.txt 2>&1

# Search for warnings
grep -i "xceed\|fluent assertions\|license" test-output.txt

# Expected: No matches (clean output)
```

---

## Rollback Strategy

### Immediate Rollback (During Migration)

If tests fail for a specific file:
1. `git revert <commit-hash>` - Revert that file's migration
2. Investigate failure
3. Fix issue
4. Re-apply migration

### Full Rollback (Critical Issue)

If migration must be abandoned:
1. `git revert <range>` - Revert all migration commits
2. Or `git reset --hard <before-migration-commit>`
3. Re-evaluate approach
4. Create new migration plan

---

## Performance Considerations

### Assertion Performance

Both libraries have similar performance for assertions:
- FluentAssertions: ~10-50 ns per assertion
- Shouldly: ~10-50 ns per assertion

**Impact**: Negligible (tests already run in ~3-5 seconds).

### Build Time Impact

- Adding Shouldly: +0.1s (one-time package restore)
- Removing FluentAssertions: -0.1s (smaller dependency tree)

**Net Impact**: Zero.

### Test Execution Time

Migration does not change test execution time:
- Same test logic
- Same assertions (just different syntax)
- Same xUnit infrastructure

**Impact**: None.

---

## Success Metrics

### Quantitative Metrics

- ✅ 0 FluentAssertions references (code)
- ✅ 0 FluentAssertions references (csproj)
- ✅ 150+ tests passing
- ✅ 0 warnings in test output
- ✅ 12 files migrated
- ✅ ~231 assertions migrated

### Qualitative Metrics

- ✅ Clean test output (no license warnings)
- ✅ Clear commit history (atomic commits)
- ✅ Documentation up-to-date
- ✅ Developer confidence (all tests green)

---

## Timeline

| Phase | Duration | Tasks |
|-------|----------|-------|
| **1. Preparation** | 30 min | Update docs, add Shouldly package |
| **2. Migration** | 2-3 hours | Migrate 12 files, test each |
| **3. Verification** | 30 min | Full test suite, cleanup |
| **4. Merge** | 15 min | PR, review, merge |
| **Total** | **3.5-4 hours** | Complete migration |

---

## Alternatives Considered

### Alternative 1: Keep FluentAssertions

**Pros**:
- No migration effort
- Familiar syntax

**Cons**:
- License risk
- Warning pollution
- Commercial dependency

**Decision**: ❌ Not acceptable (license concerns outweigh effort)

### Alternative 2: Use xUnit.Assert

**Pros**:
- Already included (no new package)
- MIT license

**Cons**:
- Verbose syntax: `Assert.Equal(expected, actual)` vs `actual.ShouldBe(expected)`
- Less readable
- More migration effort (completely different syntax)

**Decision**: ❌ Not selected (Shouldly has better syntax)

### Alternative 3: Use NUnit.Framework

**Pros**:
- Feature-rich
- MIT license

**Cons**:
- Requires changing test framework (xUnit → NUnit)
- Massive migration effort
- Different test runner

**Decision**: ❌ Not selected (too much churn)

### Alternative 4: Use Shouldly (Selected)

**Pros**:
- MIT license (truly open-source)
- Similar syntax to FluentAssertions
- Better error messages
- Minimal migration effort
- No warnings

**Cons**:
- None significant

**Decision**: ✅ **Selected** (best balance of effort, quality, and sustainability)

---

## References

- [Shouldly Documentation](https://docs.shouldly.org/)
- [Shouldly GitHub](https://github.com/shouldly/shouldly)
- [FluentAssertions License Discussion](https://github.com/fluentassertions/fluentassertions/discussions/2543)
- [Xceed License Terms](https://xceed.com/products/unit-testing/fluent-assertions/)

---

**Design Version**: 1.0  
**Date**: 2026-02-11  
**Status**: Approved for Implementation
