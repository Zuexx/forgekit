# workflow-toolchain Specification

## Purpose
Defines what this repository must declare about the tools its documented development workflow
depends on, so that a fresh clone — or a product generated from the template — arrives with the
workflow operational rather than merely described, and can prove it before relying on it.

## Requirements

### Requirement: Declared workflow toolchain

Every tool the repository's own workflow instructions depend on SHALL be declared in a
version-controlled file, and SHALL become available through a standard install command run at
the repository root. Availability of a declared tool SHALL NOT depend on machine-global state
outside the repository.

#### Scenario: A fresh clone obtains the toolchain

- **WHEN** the repository is cloned on a machine with no workflow tools installed globally, and
  the standard install command is run at the repository root
- **THEN** every declared tool is executable from within the repository
- **AND** no manual per-tool installation step is required

#### Scenario: A generated product carries the declarations

- **WHEN** a product is generated from the template
- **THEN** the generated project contains the same toolchain declarations as the base
- **AND** running the standard install command in it obtains the same tools

#### Scenario: The repository's scripts are invoked where file modes are not preserved

- **WHEN** a product is generated from the template, which does not carry the executable bit
- **THEN** the repository's scripts are still invocable through a declared command
- **AND** no manual permission change is required to run them

### Requirement: Hooks are reported when they cannot fire

Git ignores a hook that is not executable, without reporting anything. Where the repository
relies on a hook as a gate, the preflight check SHALL verify the hook can actually run, not
only that hooks are configured.

#### Scenario: The hooks path is set but the hook cannot execute

- **WHEN** `core.hooksPath` points at the repository's hooks directory but a hook file is not
  executable
- **THEN** the preflight check reports it as a failure naming the hook
- **AND** its output gives the command that makes it executable

#### Scenario: Hooks are configured and executable

- **WHEN** `core.hooksPath` is set and every hook in it is executable
- **THEN** the preflight check reports the hooks as passing

### Requirement: Cited capabilities resolve

Every skill, command, plugin, tool, and referenced document named in the repository's workflow
instruction files SHALL resolve to something present. An instruction file SHALL NOT name a
capability the declared toolchain does not provide, nor point at a document that does not exist.

#### Scenario: A named capability is missing

- **WHEN** the workflow instructions name a capability that does not resolve
- **THEN** the preflight check reports it as a failure, naming the capability and the file
  citing it

#### Scenario: A referenced document has been moved or deleted

- **WHEN** the workflow instructions point at a document path that no longer exists
- **THEN** the preflight check reports it as a failure

#### Scenario: A citation is of an unrecognised kind

- **WHEN** the workflow instructions cite something that matches none of the known kinds
- **THEN** the preflight check reports it as unrecognised rather than passing it silently

#### Scenario: All named capabilities resolve

- **WHEN** every capability named in the workflow instructions resolves
- **THEN** the preflight check reports no capability failures

### Requirement: Workflow preflight

The repository SHALL provide a single command that reports whether the workflow is operational
on the current machine. It SHALL check that the declared tools are present, that the repository's
workflow configuration is readable by the installed tool version, that the code index the
instructions require exists and reflects the current source, and that the repository's git hooks
are enabled. Each failure SHALL be reported with the command that resolves it.

Index staleness SHALL be judged against the source the index describes, not against the commit
history. Committing changes no source file, so a check anchored on commit time would report a
correct index as stale and train its readers to ignore it.

#### Scenario: The workflow is operational

- **WHEN** the preflight command runs against a correctly set up clone
- **THEN** it exits zero and reports each check as passing

#### Scenario: A prerequisite is missing

- **WHEN** the preflight command runs where a declared tool, the code index, or the git hooks
  configuration is absent
- **THEN** it exits non-zero
- **AND** its output names each failing check and the command that fixes it

#### Scenario: The code index is stale

- **WHEN** the code index exists but is older than the most recently modified source file
- **THEN** the preflight command reports it as failing rather than passing
- **AND** its output states that impact analysis based on it would be out of date

#### Scenario: A commit is made without editing source

- **WHEN** the code index reflects every source file and a commit is then made
- **THEN** the preflight command still reports the index as current

### Requirement: A check that cannot measure its subject fails

Every check SHALL report success only after examining the thing it guards. Where a check cannot
reach its subject — a tool absent, an enumeration empty, a file unreadable — it SHALL fail. A
reported `ok` means "I looked and it was fine", never "I found nothing to look at".

#### Scenario: A check has nothing to examine

- **WHEN** a check's subject cannot be enumerated or read
- **THEN** the check reports failure, naming what it could not determine

#### Scenario: A citation that resolves to nothing

- **WHEN** an instruction file cites something no check knows how to resolve
- **THEN** it is reported as unresolved rather than passed over, provided it is shaped like a
  capability rather than like ordinary prose or an identifier

### Requirement: Configuration verified by reading it back

The preflight check SHALL confirm the repository's workflow configuration is usable by reading
its values back through the installed tool and asserting they are present, rather than by
comparing a declared version against an expected one. A configuration that parses without error
but yields no values SHALL be reported as failing.

#### Scenario: Configuration silently yields nothing

- **WHEN** the workflow configuration is well-formed but a section produces no values through
  the installed tool
- **THEN** the preflight check reports that section as failing

#### Scenario: An unrecognised tool version

- **WHEN** the installed tool version differs from the one recorded but still returns the
  expected configuration values
- **THEN** the preflight check reports the configuration as passing

### Requirement: Version policy for workflow tools

Declared workflow tools SHALL track the newest release compatible with the repository's
workflow configuration, and SHALL NOT adopt a release that could change that configuration's
format without an explicit decision. The reasoning behind each tool's version policy SHALL be
recorded alongside the repository's other dependency constraints.

#### Scenario: A compatible release is published

- **WHEN** a tool publishes a release compatible with the current configuration format
- **THEN** the declared policy permits it without a change to the declaration

#### Scenario: A format-breaking release is published

- **WHEN** a tool publishes a release that could change the configuration format
- **THEN** the declared policy excludes it until it is adopted deliberately

### Requirement: Scope interview precedes change proposals

The workflow SHALL provide a tool that turns an underspecified request into a written scope
decision before any change proposal is created, and the workflow instructions SHALL describe it
at that position. It SHALL be declared in the toolchain rather than left to be installed when
first needed.

#### Scenario: An underspecified request arrives

- **WHEN** a request is too vague for a proposal to state what is included and excluded
- **THEN** the workflow instructions direct the scope interview to be run first
- **AND** its written output is available as input to the change proposal

#### Scenario: The interview tool is described accurately

- **WHEN** the workflow instructions refer to the scope interview tool
- **THEN** they name it by the identifier that invokes it
- **AND** they describe how it is invoked, rather than presenting it as an agent-invoked skill
