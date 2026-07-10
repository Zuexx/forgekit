# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records (ADRs) documenting the architectural patterns and decisions made in modern .NET applications.

**Goal:** Capture design decisions in a permanent, discoverable format that explains not just *what* we decided, but *why* we made that choice, what alternatives we considered, and when the pattern is appropriate.

## What is an ADR?

An ADR is a brief document that records an important architectural decision made along with its context and consequences. Each ADR:

- Explains a **generic pattern** applicable to many projects (not ForgeKit-specific)
- Documents **context** (the problem being solved)
- States the **decision** clearly
- Explains the **rationale** (why it's a good approach)
- Discusses **alternatives** considered and their trade-offs
- Describes **consequences** (benefits and drawbacks)
- Provides a **ForgeKit implementation example** showing how this pattern is used in practice
- Clarifies **when to use** and **when NOT to use** the pattern

## Index of ADRs

| # | Title | Status | Scope |
|---|-------|--------|-------|
| [001](#adr-001-modulefeature-pattern) | Module/Feature Pattern | ACCEPTED | Service Organization |
| [002](#adr-002-soft-delete-with-restore) | Soft-Delete with Restore | ACCEPTED | Data Deletion |
| [003](#adr-003-validation-in-pipeline) | Validation in Pipeline | ACCEPTED | Data Validation |
| [004](#adr-004-ef-core-over-repositories) | EF Core Over Repositories | ACCEPTED | Data Access |
| [005](#adr-005-audit-context-service) | Audit Context Service | ACCEPTED | Audit Trail |
| [006](#adr-006-error-response-standardization) | Error Response Standardization | ACCEPTED | Error Handling |
| [007](#adr-007-unit-of-work-pattern) | Unit of Work Pattern | ACCEPTED | Transaction Management |

---

## ADR-001: Module/Feature Pattern

**File:** [001-module-feature-pattern.md](001-module-feature-pattern.md)

**Quick Summary:** Organize large applications by feature/module rather than strict layers. Use reflection-based service discovery to automatically register modules and endpoints.

**When to use:** Medium-large applications with multiple independent features where teams work in parallel

**Key benefit:** Loose coupling between features, easy to add new modules, teams can work independently

---

## ADR-002: Soft-Delete with Restore

**File:** [002-soft-delete-with-restore.md](002-soft-delete-with-restore.md)

**Quick Summary:** Implement logical deletion (soft-delete) using an `IsDeleted` flag with automatic query filtering and restore capability within a grace period.

**When to use:** Systems requiring data recovery, compliance/GDPR support, user-initiated deletions that might be reversed

**Key benefit:** Data recovery without complexity of event sourcing, automatic filtering prevents accidental exposure

---

## ADR-003: Validation in Pipeline

**File:** [003-validation-in-pipeline.md](003-validation-in-pipeline.md)

**Quick Summary:** Centralize validation in the MediatR pipeline using a `ValidationBehavior` that runs before command/query handlers, ensuring consistent validation across all endpoints.

**When to use:** Any MediatR-based application requiring consistent validation and error handling

**Key benefit:** DRY principle, consistent error format, separation of validation from business logic

---

## ADR-004: EF Core Over Repositories

**File:** [004-ef-core-over-repositories.md](004-ef-core-over-repositories.md)

**Quick Summary:** Use EF Core DbContext directly in services rather than abstracting behind a Repository pattern. Leverage EF Core's LINQ support and query composition.

**When to use:** .NET applications where ORM flexibility and LINQ composability are important

**Key benefit:** Less boilerplate, better LINQ support, simpler debugging, better query composition

---

## ADR-005: Audit Context Service

**File:** [005-audit-context-service.md](005-audit-context-service.md)

**Quick Summary:** Implement an `IAuditContext` service that automatically populates audit fields (CreatedBy, UpdatedBy, CreatedAt, UpdatedAt) from HttpContext claims without requiring explicit parameters.

**When to use:** Applications requiring automatic audit trail population from user context

**Key benefit:** No boilerplate in every service method, automatic audit trail, claim-based user identification

---

## ADR-006: Error Response Standardization

**File:** [006-error-response-standardization.md](006-error-response-standardization.md)

**Quick Summary:** Use a standardized error response format with machine-readable error codes, field-level validation errors, and request tracing IDs for correlation.

**When to use:** Any API where clients need consistent error handling and you need error correlation for debugging

**Key benefit:** Programmatic error handling on client side, better debugging via correlation IDs, field-level validation details

---

## ADR-007: Unit of Work Pattern

**File:** [007-unit-of-work-pattern.md](007-unit-of-work-pattern.md)

**Quick Summary:** Implement a Unit of Work pattern to manage transaction boundaries and ensure atomic operations across multiple entities in a single request.

**When to use:** Applications requiring explicit transaction management and consistency guarantees across multiple operations

**Key benefit:** Explicit transaction boundaries, rollback on failure, atomic operations, clear transaction management

---

## How to Use These ADRs

1. **For New Developers:** Start with this README to understand the architectural patterns used in your project.

2. **For Design Decisions:** When deciding how to implement a feature, check the relevant ADR to understand the pattern and alternatives.

3. **For Onboarding:** Reference specific ADRs when teaching new team members why code is structured a certain way.

4. **For Code Review:** Link to ADRs when explaining architectural decisions during code review.

5. **For Other Projects:** Use these ADRs as templates for implementing similar patterns in other .NET applications.

## ADR Format

All ADRs follow this format:

1. **Context** - Generic problem statement
2. **Decision** - The approach we chose
3. **Rationale** - Why it's the best choice
4. **Alternatives Considered** - Other options and why we didn't choose them
5. **Consequences** - Benefits and drawbacks
6. **When to Use** - Applicable scenarios
7. **When NOT to Use** - Scenarios where alternatives are better
8. **ForgeKit Implementation Example** - Real code showing how ForgeKit uses this pattern
9. **Related ADRs** - Links to related decisions

See [template.md](template.md) for the full template.

## Adding New ADRs

When adding a new ADR:

1. Use the next available number (ADR-008, ADR-009, etc.)
2. Follow the [template.md](template.md) format
3. Ensure the **context is generic**, not ForgeKit-specific
4. Include at least 3 alternatives considered
5. Provide a concrete ForgeKit implementation example
6. Link from this README
7. Reference in related ADRs

## Status Legend

- **PROPOSED** - Decision under discussion
- **ACCEPTED** - Decision approved and implemented
- **SUPERSEDED** - Decision replaced by a newer ADR
- **DEPRECATED** - Decision is no longer recommended

## Related Documentation

- [Exception Handling Standardization](../ADR_EXCEPTION_HANDLING.md)
- [API Errors Guide](../API_ERRORS.md)
- [Result Pattern Guide](../RESULT_PATTERN_GUIDE.md)
- [Exception Handling Guide](../EXCEPTION_HANDLING_GUIDE.md)

## Discussion & Questions

Have questions about a pattern? Want to propose a new ADR? Start a discussion!

---

**Last Updated:** 2026-02-09
**Next Review:** Quarterly or when significant architectural changes are made
