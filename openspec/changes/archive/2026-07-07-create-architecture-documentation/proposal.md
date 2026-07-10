# Proposal: Create Architecture Decision Records (ADRs)

**Created:** 2026-02-09  
**Status:** COMPLETED  
**Priority:** Low (Documentation)  
**Effort:** Medium (8-10 hours)  
**Completed:** 2026-02-09

## Overview

Document the excellent architectural decisions already implemented in the Scaffold API codebase through a series of Architecture Decision Records (ADRs). These documents explain the rationale, alternatives considered, and outcomes for key design patterns and decisions.

ADRs will be written in a **generic, reusable format** - explaining the patterns themselves rather than Scaffold-specific implementations. This allows other projects to learn from and adopt similar patterns.

## Why

**Problem:** 
- No formal documentation of why certain architectural patterns were chosen
- Team members lack context for design decisions
- New team members don't understand the reasoning behind patterns
- Other teams can't easily reuse/learn from the patterns
- Difficult to evaluate if alternatives should be reconsidered

**Impact:** 
- Slower onboarding of new developers
- Risk of inconsistent implementation of patterns
- Lost institutional knowledge
- Cannot share architectural knowledge across projects
- Harder to maintain consistency as codebase evolves

**Solution:** Create comprehensive ADRs documenting generic architectural patterns with rationale, alternatives, and outcomes. Patterns explained in a reusable way that applies to other projects.

## Problem Statement

### Current Strengths (Well Implemented)
- ✅ **Module/Feature Pattern** - Clean reflection-based service registration
- ✅ **EF Core DbContext** - World-class configuration with global filters and specifications
- ✅ **ValidationBehavior** - Centralized validation in MediatR pipeline
- ✅ **Base Entity Pattern** - Unified audit and soft-delete foundation
- ✅ **Soft-Delete with Restore** - Logical deletion with grace period restore capability
- ✅ **Audit Context Service** - Automatic audit trail population from HttpContext claims
- ✅ **Error Response Standardization** - Machine-readable error codes with field-level details
- ✅ **Unit of Work Pattern** - Transaction management and consistency enforcement

### Missing Generic Documentation
- ❌ No generic ADR on module/feature pattern (applicable to any .NET app)
- ❌ No generic ADR on soft-delete strategy (applicable to any data-driven app)
- ❌ No generic ADR on validation pipeline (applicable to any MediatR app)
- ❌ No generic ADR on EF Core approach (applicable to any EF Core project)
- ❌ No generic ADR on error handling (applicable to any API)
- ❌ No generic ADR comparing alternatives (repository vs direct DbContext, etc.)
- ❌ No generic ADR on audit patterns (applicable to compliance-heavy applications)

## Solution Overview

### 1. Create Generic ADR Directory Structure
```
openspec/
└─ adr/
   ├─ README.md (ADR index and overview)
   ├─ 001-module-feature-pattern.md
   ├─ 002-soft-delete-with-restore.md
   ├─ 003-validation-in-pipeline.md
   ├─ 004-ef-core-over-repositories.md
   ├─ 005-audit-context-service.md
   ├─ 006-error-response-standardization.md
   └─ 007-unit-of-work-pattern.md
```

### 2. Generic ADR Format
Each ADR will be written in a **generic, reusable format** with:
- **Context:** General problem that applies to many projects
- **Decision:** The pattern/approach
- **Rationale:** Why it's a good approach
- **Alternatives Considered:** Generic alternatives with trade-offs
- **Consequences:** Benefits and drawbacks
- **Scaffold Implementation Example:** Code example from this project (but not Scaffold-specific)
- **When to Use:** Scenarios where this pattern applies
- **When NOT to Use:** Scenarios where alternatives might be better

### 3. Generic Focus
Each ADR will:
- Explain the pattern generically (works for any project)
- Show how Scaffold implements it as one example
- Explain trade-offs that apply universally
- Discuss variations for different scenarios
- Avoid Scaffold-specific terminology/entities

Example structure:
```
# ADR-002: Soft-Delete with Restore Capability

## Context
Many systems need to support data deletion for compliance/GDPR while maintaining ability to
restore accidentally deleted data. Physical deletion is permanent; soft-delete allows recovery.

## Decision
Implement soft-delete using:
1. IsDeleted flag on entities
2. Global query filters to exclude deleted records automatically
3. Restore capability with configurable grace period
4. Separate audit trail for deletion events

## Rationale
- Allows data recovery without complexity of full event sourcing
- Automatic query filtering prevents accidental exposure of deleted data
- Grace period balances recovery needs with data minimization
- Works with any ORM supporting query filters

## When to Use
- Legal/compliance requirements for data recovery
- User-initiated deletions that might be reversed
- Audit requirements for delete tracking

## Scaffold Implementation Example
TodoItem entity with 30-day restore grace period...
```

## Success Criteria

- ✅ 7 generic ADRs created (applicable to any project)
- ✅ Each ADR includes: Context, Decision, Rationale, Alternatives, Consequences, When to Use
- ✅ Generic examples that apply universally
- ✅ Scaffold implementation examples (but framed as examples, not Scaffold-specific)
- ✅ ADR README provides navigation and quick reference
- ✅ All ADRs follow consistent generic format
- ✅ Documentation suitable for knowledge sharing across teams/projects

## Implementation Phases

| Phase | Title | Effort | Duration |
|-------|-------|--------|----------|
| 1 | Create ADR directory, template, and README | 1 hour | 1 day |
| 2 | Write 7 generic ADRs (generic context + Scaffold examples) | 5 hours | 3 days |
| 3 | Review for generic applicability | 1.5 hours | 1 day |
| 4 | Team review and refinement | 1.5 hours | 1 day |

## Risk Assessment

- **Low Risk:** Documentation only, no code changes
- **Mitigation:** ADRs document existing patterns, no changes to implementation
- **Effort:** Medium (8-10 hours for comprehensive generic documentation)

## Rollback Plan

If needed:
1. ADRs are standalone documentation
2. Can be modified/removed without affecting codebase
3. No dependencies on documentation for functionality

## Notes

- **Generic Focus:** Patterns explained for any project, not Scaffold-specific
- **Reusable:** Other teams can learn and adopt these patterns
- **Flexible:** Each ADR discusses variations and when to use alternatives
- **Practical:** Includes real implementation examples from Scaffold
- **Knowledge Sharing:** Creates institutional knowledge that's shareable

## Scope Exclusion

This proposal focuses on **generic architectural patterns**, not:
- Scaffold business logic documentation
- Scaffold domain models
- Scaffold-specific features
- Scaffold workflows

## Next Steps After Implementation

1. Link ADRs from main README.md
2. Reference ADRs in code as patterns evolve
3. Share ADRs with other teams for pattern adoption
4. Create onboarding guide referencing key ADRs
5. Use ADRs as basis for architectural consistency guidelines
