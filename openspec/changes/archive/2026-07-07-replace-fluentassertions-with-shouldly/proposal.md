# Change: Replace FluentAssertions with Shouldly

## Why

FluentAssertions has transitioned from open-source to a **paid commercial license** model under Xceed. While allowed for non-commercial use, this creates several concerns:

### Legal & Compliance Issues
- **License Risk**: Commercial use now requires paid subscription, introducing legal liability
- **Audit Trail**: Warning messages in test output pollute CI/CD logs and create noise
- **Compliance**: Corporate environments may flag commercial dependencies in OSS projects

### Technical Issues
- **Test Output Pollution**: License warnings appear in every test run:
  ```
  Warning:
  The component "Fluent Assertions" is governed by the rules defined in the Xceed License Agreement
  and the Xceed Fluent Assertions Community License. You may use Fluent Assertions free of charge
  for non-commercial use only...
  ```
- **CI/CD Impact**: Warning messages clutter build logs, making real issues harder to spot
- **Uncertainty**: Future license changes or pricing models are unpredictable

### Strategic Reasons
- **Open-Source Commitment**: This project should use truly open-source dependencies
- **Long-Term Sustainability**: Avoid vendor lock-in for a core testing library
- **Community Trust**: Using MIT-licensed alternatives aligns with OSS principles

### Why Shouldly?

**Shouldly** is the ideal replacement:
- ✅ **MIT License** - Truly open-source, no commercial restrictions
- ✅ **Active Maintenance** - Regularly updated, 7k+ stars on GitHub
- ✅ **Similar API** - Minimal migration effort, natural syntax
- ✅ **Better Error Messages** - Human-readable assertion failures
- ✅ **No Warnings** - Clean test output
- ✅ **Battle-Tested** - Used by many large OSS projects

**Example Comparison:**
```csharp
// FluentAssertions
result.Should().BeOfType<Success>();
result.Data.Should().Be("expected");

// Shouldly (very similar!)
result.ShouldBeOfType<Success>();
result.Data.ShouldBe("expected");
```

## What Changes

### NuGet Packages
- **Remove**: `FluentAssertions` (v8.8.0) from Api.Tests.csproj
- **Add**: `Shouldly` (latest stable version) to Api.Tests.csproj

### Test Files (231 usages across 12 files)
Replace FluentAssertions assertions with Shouldly equivalents:

| FluentAssertions | Shouldly | Count |
|------------------|----------|-------|
| `Should().Be()` | `ShouldBe()` | ~80 |
| `Should().BeOfType<T>()` | `ShouldBeOfType<T>()` | ~40 |
| `Should().NotBeNull()` | `ShouldNotBeNull()` | ~30 |
| `Should().BeTrue()` / `BeFalse()` | `ShouldBeTrue()` / `ShouldBeFalse()` | ~20 |
| `Should().Contain()` | `ShouldContain()` | ~15 |
| `Should().BeEmpty()` | `ShouldBeEmpty()` | ~10 |
| `Should().StartWith()` | `ShouldStartWith()` | ~5 |
| `Should().HaveCount()` | `ShouldBe()` (with `.Count`) | ~5 |
| Others (Throw, Match, etc.) | Equivalent Shouldly methods | ~26 |

### Affected Test Files (12 files)
1. `Api.Tests/Modules/SampleResourceModuleTests.cs` - 10 usages
2. `Api.Tests/Validators/CreateResourceCommandValidatorTests.cs` - 1 usage
3. `Api.Tests/Exceptions/ExceptionTypesTests.cs` - 14 usages
4. `Api.Tests/Samples/SampleHandlerTests.cs` - 33 usages
5. `Api.Tests/Data/UnitOfWorkTests.cs` - 19 usages
6. `Api.Tests/Middlewares/CorrelationIdMiddlewareTests.cs` - 19 usages
7. `Api.Tests/Results/ResultTests.cs` - 44 usages
8. `Api.Tests/Integration/Middlewares/ExceptionHandlingIntegrationTests.cs` - 17 usages
9. `Api.Tests/Middlewares/ExceptionHandlingMiddlewareTests.cs` - 24 usages
10. `Api.Tests/Integration/Data/UnitOfWorkIntegrationTests.cs` - 11 usages
11. `Api.Tests/Handlers/ResultHandlerTests.cs` - 24 usages
12. `Api.Tests/Handlers/ResultHandlerLoggingTests.cs` - 15 usages

### Documentation Updates
- **docs/FLUENT_VALIDATION_GUIDE.md**: Update validator testing examples
- **docs/EXTENDING_THE_API.md**: Update testing section examples
- **docs/USER_GUIDE.md**: Update testing guide if FluentAssertions is mentioned
- **README.md**: Update if FluentAssertions is listed in tech stack

## Impact

### Affected Specs
- **specs/testing/spec.md** (if exists) - Update testing conventions

### Affected Code
- **Api.Tests.csproj** - Package reference change
- **All test files** - Assertion syntax migration (12 files, ~231 assertions)
- **Documentation** - Testing examples and guides

### Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| Tests fail after migration | Medium | Run full test suite after each file migration |
| Missed assertions | Low | Search for `using FluentAssertions` and `.Should` patterns |
| Documentation outdated | Low | Systematic review of all docs mentioning FluentAssertions |
| Developer confusion | Low | Clear commit messages, update guides first |

### Test Coverage Impact
- **No change** - Same number of tests, same coverage
- **Validation** - All 150+ tests must pass after migration

## Breaking Changes

**None** - This is a test-only change. Production code is unaffected.

### Consumer Impact
- **API Consumers**: No impact
- **Contributors**: Must use Shouldly syntax for new tests
- **CI/CD**: Cleaner output (no license warnings)

## Dependencies

### Removed
- `FluentAssertions` v8.8.0

### Added
- `Shouldly` (latest stable, ~v4.2+)

### Compatibility
- ✅ .NET 10 compatible
- ✅ xUnit compatible
- ✅ No conflicts with existing test infrastructure

## Implementation Strategy

### Phase 1: Preparation
1. Update documentation first (show new syntax)
2. Add Shouldly package to Api.Tests.csproj
3. Keep FluentAssertions temporarily (allow both to coexist during migration)

### Phase 2: Migration (File-by-File)
For each test file:
1. Add `using Shouldly;`
2. Remove `using FluentAssertions;`
3. Replace assertions (use find/replace with regex)
4. Run tests for that file
5. Commit when tests pass

### Phase 3: Cleanup
1. Verify all test files migrated (search for `FluentAssertions` references)
2. Remove FluentAssertions package from csproj
3. Run full test suite
4. Verify no warnings in test output

### Rollback Plan
If issues arise:
1. Revert commits file-by-file
2. Keep FluentAssertions until all issues resolved
3. Fix issues and retry migration

## Success Criteria

✅ All 150+ tests pass  
✅ No FluentAssertions references in code  
✅ No FluentAssertions package in csproj  
✅ No license warnings in test output  
✅ Documentation updated with Shouldly examples  
✅ CI/CD pipeline green  

## Timeline

- **Preparation**: 30 minutes (docs, package setup)
- **Migration**: 2-3 hours (12 files, ~231 assertions)
- **Verification**: 30 minutes (test runs, cleanup)
- **Total**: ~3-4 hours

## Alternatives Considered

| Library | License | Pros | Cons | Decision |
|---------|---------|------|------|----------|
| **Shouldly** | MIT | Clean syntax, active, no warnings | None significant | ✅ **Selected** |
| xUnit.Assert | MIT | Already included | Verbose syntax | ❌ Not selected |
| NUnit.Framework | MIT | Feature-rich | Requires test framework change | ❌ Not selected |
| Keep FluentAssertions | Commercial | No migration effort | License risk, warnings | ❌ Not acceptable |

## References

- [Shouldly GitHub](https://github.com/shouldly/shouldly)
- [Shouldly Documentation](https://docs.shouldly.org/)
- [FluentAssertions License Change](https://github.com/fluentassertions/fluentassertions/discussions/2543)
- [Shouldly vs FluentAssertions Comparison](https://docs.shouldly.org/documentation/getting-started)

---

**Proposal Status**: Pending Approval  
**Created**: 2026-02-11  
**Author**: Development Team  
**Estimated Effort**: 3-4 hours  
**Priority**: Medium (non-urgent, but should be done before next major release)
