# Design: Architecture Decision Records (ADRs)

**Version:** 1.0  
**Last Updated:** 2026-02-09  

## Overview

Create a comprehensive set of Architecture Decision Records that document generic architectural patterns used in the Scaffold API. Each ADR explains a pattern in a reusable way suitable for other projects while using Scaffold as a concrete implementation example.

## ADR Directory Structure

```
openspec/
└─ adr/
   ├─ README.md                                  # ADR overview and index
   ├─ template.md                                # ADR template
   ├─ 001-module-feature-pattern.md             # Module/Feature Pattern
   ├─ 002-soft-delete-with-restore.md           # Soft-Delete with Grace Period
   ├─ 003-validation-in-pipeline.md             # Validation in Pipeline
   ├─ 004-ef-core-over-repositories.md          # EF Core vs Repository
   ├─ 005-audit-context-service.md              # Automatic Audit Trails
   ├─ 006-error-response-standardization.md     # Error Response Format
   └─ 007-unit-of-work-pattern.md               # Unit of Work Pattern
```

## ADR Template

Each ADR follows this structure:

```markdown
# ADR-NNN: Title (Generic Pattern Name)

**Status:** Accepted/Proposed/Deprecated  
**Decision Date:** YYYY-MM-DD  
**Context:** Date context was created  

## Context

[Describe the generic problem/situation that many projects face]

### Why This Matters
[Explain why this is important for any project]

### Example from Scaffold
[Brief mention: "In the Scaffold system..." but generic explanation]

## Decision

[Describe the generic pattern/approach]

## Rationale

### Benefits
- Generic benefit 1
- Generic benefit 2
- Generic benefit 3

### Trade-offs
- Trade-off 1
- Trade-off 2

## Alternatives Considered

### Alternative 1: [Generic Name]
- **Pros:** [Generic pros]
- **Cons:** [Generic cons]
- **When better:** [Generic scenarios]

### Alternative 2: [Generic Name]
- **Pros:** [Generic pros]
- **Cons:** [Generic cons]
- **When better:** [Generic scenarios]

## Consequences

### Positive Outcomes
- [Generic positive consequence]
- [Generic positive consequence]

### Challenges
- [Generic challenge]
- [Generic challenge]

## When to Use This Pattern

- Scenario 1 (applies to any project)
- Scenario 2 (applies to any project)
- Scenario 3 (applies to any project)

## When NOT to Use This Pattern

- Scenario 1 (when alternative is better)
- Scenario 2 (when this adds complexity)
- Scenario 3 (when simple solution exists)

## Implementation Considerations

### Design Pattern
[Generic design pattern description]

### Complexity
[Generic complexity assessment]

### Team Skills Required
[Generic skill requirements]

### Learning Resources
[Generic references/patterns]

## Scaffold Implementation Example

[Code examples from Scaffold implementation]

[Show how pattern is concretely implemented in Scaffold]

[Explain Scaffold-specific variations if any]

## Related Patterns

- Link to related ADR 1
- Link to related ADR 2

## Future Evolution

[What might change in future versions of this pattern]

## References

[Generic references, papers, patterns]
```

## Planned ADRs

### ADR-001: Module/Feature Pattern
**Focus:** Generic modular service organization
- Explain why projects benefit from organizing code by feature/module
- Show how reflection-based registration works generically
- Scaffold example: IModule interface with RegisterModule()
- When to use: Any large .NET application
- Benefits: Loose coupling, independent feature teams, clean organization

### ADR-002: Soft-Delete with Restore
**Focus:** Generic soft-delete pattern with grace period
- Explain why systems need recoverable deletions
- Show how global query filters provide transparency
- Scaffold example: TodoItem with 30-day restore grace period
- When to use: Compliance-heavy systems, user-initiated deletions
- Benefits: Data recovery, audit trail, GDPR compliance
- Trade-offs: Extra complexity, storage overhead

### ADR-003: Validation in Pipeline
**Focus:** Centralized validation in command/request pipeline
- Explain why validation in middleware improves consistency
- Show MediatR behavior pipeline for validation
- Scaffold example: ValidationBehavior with FluentValidation
- When to use: Any MediatR-based application
- Benefits: Cross-cutting concern, consistent error format, single place to change

### ADR-004: EF Core Over Repositories
**Focus:** Direct DbContext usage vs Repository abstraction
- Explain trade-offs between abstractions and simplicity
- Show why Repository can be premature abstraction
- Scaffold example: Direct DbContext in services, no Repository
- When to use: When ORM is stable and well-documented
- Benefits: Simpler code, better LINQ support, less boilerplate
- Trade-offs: Harder to mock in tests, ORM dependency

### ADR-005: Audit Context Service
**Focus:** Automatic audit trail population from HTTP context
- Explain why capturing "who did what" is important
- Show pattern of extracting user from claims
- Scaffold example: IAuditContext from ClaimsPrincipal
- When to use: Any system with compliance requirements
- Benefits: Consistent audit trail, automatic population, no manual user tracking
- Trade-offs: Requires authentication/claims setup

### ADR-006: Error Response Standardization
**Focus:** Machine-readable error codes with field-level details
- Explain why consistent error format improves client experience
- Show RFC 7807 Problem Details approach
- Scaffold example: ErrorResponse with Code, Timestamp, TraceId, Errors
- When to use: Any API with diverse error scenarios
- Benefits: Programmatic error handling, better debugging, consistent UX
- Trade-offs: Extra fields in response, client parsing complexity

### ADR-007: Unit of Work Pattern
**Focus:** Transaction management and consistency
- Explain why atomic operations matter
- Show pattern for managing transactions
- Scaffold example: IUnitOfWork with BeginTransaction, CommitTransaction
- When to use: Multi-entity operations requiring consistency
- Benefits: Data consistency, atomic saves, easy rollback
- Trade-offs: Transaction management complexity

## README.md Structure

```markdown
# Architecture Decision Records (ADRs)

This directory contains ADRs documenting architectural patterns used in the Scaffold system.
ADRs are written in a **generic, reusable format** applicable to any .NET project.

## Quick Reference

| # | Title | Pattern Type | Complexity |
|---|-------|--------------|-----------|
| 001 | Module/Feature Pattern | Organization | Medium |
| 002 | Soft-Delete with Restore | Data | Medium |
| 003 | Validation in Pipeline | Cross-cutting | Low |
| 004 | EF Core Over Repositories | Architecture | Medium |
| 005 | Audit Context Service | Data | Medium |
| 006 | Error Response Standardization | API | Low |
| 007 | Unit of Work Pattern | Transactions | Medium |

## Reading Guide

### For New Team Members
Start with: 001 → 003 → 006 → [specific area of interest]

### For Architecture Decisions
Focus on: 004 (ORM choice) → 002 (soft-delete) → 007 (transactions)

### For API Development
Focus on: 006 (errors) → 003 (validation) → 001 (modules)

## How ADRs Are Used

1. **Design Decisions:** Reference ADRs when making similar choices
2. **Code Reviews:** Link ADRs in comments to explain patterns
3. **Onboarding:** New developers read relevant ADRs
4. **Knowledge Sharing:** Other teams learn from our decisions
5. **Evolution:** ADR status changes as patterns evolve

## Creating New ADRs

When a new architectural decision needs documentation:
1. Use the template (template.md)
2. Focus on generic pattern, not Scaffold-specific
3. Include concrete examples
4. Explain alternatives and trade-offs
5. Link related ADRs
6. Submit for team review

## Status Values

- **Accepted:** Pattern is actively used in production
- **Proposed:** Pattern under consideration
- **Deprecated:** Pattern being phased out
- **Superseded:** Replaced by newer ADR (link to replacement)

## Learning More

[Links to referenced books, papers, patterns]
```

## Implementation Approach

### Step 1: Create Infrastructure
- Create openspec/adr directory
- Create README.md with overview and index
- Create template.md as reference

### Step 2: Write Generic ADRs
- Focus on pattern explanation first (generic)
- Add Scaffold as example of implementation
- Compare with alternatives
- Explain trade-offs universally applicable
- Include learning resources

### Step 3: Quality Review
- Ensure each ADR is understandable to someone outside Scaffold
- Verify examples aren't too Scaffold-specific
- Check that patterns are truly documented
- Validate consistency across ADRs

### Step 4: Integration
- Link from main README.md
- Reference in code when pattern is used
- Use in onboarding documentation
- Share with other teams

## Benefits of Generic Documentation

✅ **Reusable Knowledge** - Other projects can adopt patterns  
✅ **Better Decisions** - Clear rationale for design choices  
✅ **Onboarding** - New developers understand architecture quickly  
✅ **Knowledge Sharing** - Cross-team learning and consistency  
✅ **Quality** - Well-documented patterns lead to better implementations  
✅ **Future Evolution** - Clear baseline for architectural changes  

## Success Metrics

- 7 ADRs completed
- Each ADR includes 4+ alternatives
- All team members aware of relevant ADRs
- New code references ADRs in comments
- Other teams adopt patterns from ADRs
