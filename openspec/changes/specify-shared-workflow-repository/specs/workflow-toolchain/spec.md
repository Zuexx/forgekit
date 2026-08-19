## ADDED Requirements

### Requirement: Shared workflow files are owned upstream

A defined set of workflow files SHALL be owned by a single upstream repository and delivered to
each consuming repository by a sync command rather than maintained separately in each. A
consuming repository SHALL NOT be the place those files are edited: the sync overwrites them, so
a local edit survives only until the next sync and then disappears without any report.

Each consuming repository SHALL retain sole ownership of the parts that differ between stacks —
its own project context, its own verification command, and its own dependency manifest.

The repository SHALL document which files are shared and which it owns, so that the boundary is
discoverable without reading the sync implementation.

#### Scenario: A shared file is edited locally

- **WHEN** a shared workflow file is edited in a consuming repository and the sync is then run
- **THEN** the upstream version replaces the local edit
- **AND** the documented boundary identifies that file as one owned upstream

#### Scenario: A repository-owned file is left alone

- **WHEN** the sync runs in a consuming repository
- **THEN** the repository's own project context, verification command, and dependency manifest
  are unchanged

### Requirement: Synchronising the workflow is repeatable

The sync SHALL produce the same result whether it is run once or many times. Where it merges
shared content into a file the repository also owns, it SHALL replace the previously merged
region rather than append alongside it, and SHALL leave the repository's own content in that
file untouched.

The sync SHALL report each file it updated, and SHALL report rather than silently skip a shared
file that upstream no longer publishes.

#### Scenario: The sync runs twice

- **WHEN** the sync is run and then run again with no upstream change
- **THEN** the second run leaves the repository in the same state as the first
- **AND** the merged region appears exactly once

#### Scenario: A shared file no longer exists upstream

- **WHEN** the sync runs and a file it expects is absent upstream
- **THEN** it reports that file as missing rather than leaving a stale copy unremarked

### Requirement: A failed merge leaves the previous configuration intact

Where the sync merges shared content into the repository's workflow configuration, it SHALL
verify the merged result before replacing the existing file. If the result does not contain the
sections the merge exists to deliver, the sync SHALL fail and leave the existing file unchanged.

A partially merged configuration is worse than an unmerged one: the tooling reads it without
error and produces nothing, which is the failure mode the configuration readback requirement
exists to catch.

#### Scenario: The merged result is missing its sections

- **WHEN** the merged configuration would not contain the shared sections
- **THEN** the sync reports a failure
- **AND** the repository's existing configuration file is left as it was

### Requirement: Stack-specific inputs are declared by the repository

The preflight check SHALL be usable without modification across repositories of different
technology stacks. Every input that varies by stack — which files count as source, which
machine-level tools the stack requires, and which nested package directories exist — SHALL be
declared by the repository rather than written into the shared check.

A tool a repository declares as a stack requirement SHALL count as part of that repository's
declared toolchain wherever instruction files cite it, so that a correct citation of a real tool
is not reported as unresolved.

Machine-level tools SHALL be reported as resolved against the machine rather than against the
repository's own toolchain, because that is a weaker guarantee and the report is the only place
a reader can learn which kind of answer they were given.

#### Scenario: The same check runs against a different stack

- **WHEN** the preflight check runs in a repository whose declaration names a different set of
  source file types and machine-level tools
- **THEN** it measures that repository's sources and tools without the check itself differing

#### Scenario: An instruction file cites a declared stack tool

- **WHEN** an instruction file cites a tool the repository declares as a stack requirement
- **THEN** the citation resolves rather than being reported as outside the declared toolchain

## MODIFIED Requirements

### Requirement: A check that cannot measure its subject fails

Every check SHALL report success only after examining the thing it guards. Where a check cannot
reach its subject — a tool absent, an enumeration empty, a file unreadable — it SHALL fail. A
reported `ok` means "I looked and it was fine", never "I found nothing to look at".

Where a check depends on a declaration the repository supplies, an absent or empty declaration
SHALL be treated as an inability to measure and reported as a failure. It SHALL NOT fall back to
a default that measures something broader, because a check that quietly changes its own subject
reports a result no reader can interpret.

#### Scenario: A check has nothing to examine

- **WHEN** a check's subject cannot be enumerated or read
- **THEN** the check reports failure, naming what it could not determine

#### Scenario: A citation that resolves to nothing

- **WHEN** an instruction file cites something no check knows how to resolve
- **THEN** it is reported as unresolved rather than passed over, provided it is shaped like a
  capability rather than like ordinary prose or an identifier

#### Scenario: A required declaration is missing

- **WHEN** a check depends on a repository declaration that is absent or empty
- **THEN** the check reports failure naming the missing declaration
- **AND** it does not substitute a broader subject in its place
