# architecture-documentation Specification

## Purpose
Defines documentation expectations for architecture decisions, project structure, and implementation guidance.
## Requirements
### Requirement: ADR Directory and Infrastructure

The system SHALL provide an Architecture Decision Records (ADRs) directory with standardized infrastructure for documenting architectural patterns.

**Acceptance Criteria:**
- Directory `openspec/adr/` SHALL be created
- ADR README.md SHALL include overview, index, and navigation
- ADR template.md SHALL provide standard structure
- Template SHALL include all required sections with guidance

#### Scenario: Developer Navigates ADR Documentation
- **WHEN** a developer wants to understand architectural patterns
- **THEN** they can find openspec/adr/README.md
- **AND** it provides quick reference table of all ADRs
- **AND** it explains which ADRs to read for their role
- **AND** it shows how to create new ADRs

#### Scenario: Architect Creates New ADR
- **WHEN** a new architectural decision needs documentation
- **THEN** they reference openspec/adr/template.md
- **AND** the template provides required sections
- **AND** guidance explains what each section should contain
- **AND** examples show how to write generic patterns

### Requirement: Generic Architectural Patterns Documentation

The system SHALL document 7 core architectural patterns in a generic, reusable format suitable for any .NET project.

**Acceptance Criteria:**
- 7 ADRs SHALL be created (ADR-001 through ADR-007)
- Each ADR SHALL focus on generic pattern (not ForgeKit-specific)
- Each ADR SHALL include: Context, Decision, Rationale, Alternatives, Consequences
- Each ADR SHALL include "When to Use" and "When NOT to Use"
- Each ADR SHALL provide ForgeKit implementation example
- Each ADR SHALL compare with 3+ alternative approaches
- Each ADR SHALL explain trade-offs universally applicable

#### Scenario: Module/Feature Pattern Documentation
- **WHEN** a developer needs to understand modular organization
- **THEN** they read ADR-001 about Module/Feature Pattern
- **AND** it explains benefits of modular architecture generically
- **AND** it shows alternative approaches (monolithic, package-by-layer)
- **AND** it includes ForgeKit IModule example
- **AND** it explains when this pattern is beneficial
- **AND** it explains when simpler approaches are better

#### Scenario: Soft-Delete Pattern Documentation
- **WHEN** a team needs to implement recoverable deletion
- **THEN** they read ADR-002 about Soft-Delete with Restore
- **AND** it explains grace period concept
- **THEN** it compares with physical delete, archive tables, event sourcing
- **AND** it shows ForgeKit 30-day restore example
- **AND** it explains global query filter approach
- **AND** they understand trade-offs and can decide if pattern fits

#### Scenario: Validation Pattern Documentation
- **WHEN** a team needs centralized validation approach
- **THEN** they read ADR-003 about Validation in Pipeline
- **AND** it explains MediatR pipeline benefits
- **AND** it compares with attribute-based, manual validation
- **AND** it shows ForgeKit ValidationBehavior example
- **AND** they understand why this reduces boilerplate

#### Scenario: EF Core vs Repository Decision
- **WHEN** team must decide on data access approach
- **THEN** they read ADR-004 about EF Core Over Repositories
- **AND** it explains when Repository adds value
- **AND** it explains when Repository is premature abstraction
- **AND** it compares with Generic Repository, Specification patterns
- **AND** it shows ForgeKit direct DbContext approach
- **AND** they understand testability trade-offs

#### Scenario: Audit Context Documentation
- **WHEN** team needs audit trail implementation
- **THEN** they read ADR-005 about Audit Context Service
- **AND** it explains extracting user from HTTP context
- **AND** it compares with manual parameters, triggers
- **AND** it shows ForgeKit IAuditContext example
- **AND** they understand compliance benefits

#### Scenario: Error Response Documentation
- **WHEN** team needs consistent error handling
- **THEN** they read ADR-006 about Error Response Standardization
- **AND** it explains RFC 7807 Problem Details
- **AND** it compares with generic messages, status codes only
- **AND** it shows ForgeKit ErrorResponse with codes
- **AND** they understand client-side error handling benefits

#### Scenario: Unit of Work Documentation
- **WHEN** team needs transaction management approach
- **THEN** they read ADR-007 about Unit of Work Pattern
- **AND** it explains atomic multi-entity operations
- **AND** it compares with implicit transactions, repository UoW
- **AND** it shows ForgeKit IUnitOfWork example
- **AND** they understand consistency and rollback benefits

### Requirement: ADR Quality Standards

All ADRs SHALL meet quality standards ensuring reusability and clarity.

**Acceptance Criteria:**
- Each ADR SHALL be readable by someone unfamiliar with ForgeKit
- ForgeKit SHALL be presented as example, not the subject
- Alternatives SHALL be explained generically
- Trade-offs SHALL be universally relevant
- Context SHALL address general problem categories

#### Scenario: External Team Learns from ADR
- **WHEN** a team outside ForgeKit reads an ADR
- **THEN** they understand the pattern without ForgeKit knowledge
- **AND** they can apply pattern to their own project
- **AND** ForgeKit example helps clarify but isn't required to understand

#### Scenario: Cross-Reference ADRs
- **WHEN** an ADR mentions related pattern
- **THEN** it includes link to related ADR
- **AND** terminology is consistent across ADRs
- **AND** conflicting guidance is resolved

### Requirement: ADR Index and Navigation

The openspec/adr/README.md SHALL provide clear navigation and guidance.

**Acceptance Criteria:**
- Quick reference table SHALL show all ADRs with titles and complexity
- Reading guide SHALL suggest order for different roles
- "How to Use" section SHALL explain ADR purpose
- Status values SHALL be defined (Accepted, Proposed, Deprecated)

#### Scenario: Onboarding New Developer
- **WHEN** new developer joins team
- **THEN** they read openspec/adr/README.md
- **AND** it suggests which ADRs to read for backend developer role
- **AND** suggested reading order makes sense
- **AND** they quickly understand architectural decisions

#### Scenario: Architecture Review
- **WHEN** team discusses new architectural decision
- **THEN** they reference relevant ADRs
- **AND** they follow pattern from ADRs
- **AND** consistency with existing architecture is verified
