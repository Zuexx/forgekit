# Specification: Comprehensive API Documentation

## Overview

This specification defines how XML documentation comments are written, generated, and used to provide professional API documentation through IDE IntelliSense and Scalar UI integration.

## Scope

**IN Scope:**
- XML documentation comments in source code
- Documentation content standards and patterns
- Integration with Scalar UI
- IDE IntelliSense display
- Build configuration for documentation generation

**OUT of Scope:**
- Swagger UI (not in use)
- External documentation sites
- Markdown-based API guides (separate concern)

## ADDED Requirements

### Requirement: XML Documentation Generation

The build process SHALL generate a complete `Api.xml` file containing all public API documentation.

**Specification:**
- `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in Api.csproj
- Output to: `bin\$(Configuration)\$(TargetFramework)\Api.xml`
- Include all public types and members
- Build completes without documentation-related errors

#### Scenario: Developer builds project and API.xml is generated

- **WHEN** a developer runs `dotnet build`
- **THEN** `Api.xml` file exists in the output directory
- **AND** the file contains documentation for all public handlers
- **AND** no documentation-related warnings appear in build output

### Requirement: Handler Documentation

Every public MediatR handler SHALL have comprehensive XML documentation.

**Handler Class Documentation Pattern:**
```csharp
/// <summary>One-sentence description of handler purpose.</summary>
/// <remarks>
/// Multi-paragraph explanation including:
/// - Business context
/// - Workflow or state transitions
/// - Business rules or constraints
/// - Side effects or logging
/// </remarks>
public class SomeHandler : IRequestHandler<SomeCommand, SomeResponse>
```

**Handle Method Documentation Pattern:**
```csharp
/// <summary>Processes the request.</summary>
/// <param name="request">Request object</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Response with operation result</returns>
/// <exception cref="NotFoundException">When resource not found</exception>
public async Task<SomeResponse> Handle(SomeCommand request, CancellationToken cancellationToken)
```

#### Scenario: Developer creates a new command handler

- **WHEN** a developer creates a new MediatR command handler with documentation
- **THEN** IDE tooltips display the complete documentation
- **AND** Scalar UI shows the description for the handler
- **AND** future maintainers can understand the business logic

### Requirement: Command and Query Documentation

Every Command and Query class SHALL document its purpose and all properties.

**Pattern:**
```csharp
/// <summary>Describes what this command/query does.</summary>
public class SomeCommand : ICommand<SomeResponse>
{
    /// <summary>Describes this property.</summary>
    public required string SomeField { get; set; }
}
```

#### Scenario: Developer creates a command with properties

- **WHEN** a developer creates a command class with required properties and adds documentation
- **THEN** IDE shows what the command is for
- **AND** Scalar UI displays the request schema with field descriptions

### Requirement: Response DTO Documentation

Every response DTO and its properties SHALL be fully documented.

**Pattern:**
```csharp
/// <summary>Describes what this response represents.</summary>
/// <remarks>Optional context about when/how this response is returned.</remarks>
public class SomeResponse
{
    /// <summary>Describes this property.</summary>
    public required string SomeField { get; set; }
}
```

#### Scenario: Handler returns response with multiple properties

- **WHEN** a handler returns a response DTO with all properties documented
- **THEN** developers understand what each field means
- **AND** Scalar UI shows the response schema clearly

### Requirement: Exception Documentation

Every thrown exception SHALL be documented with HTTP status and error code.

**Pattern:**
```csharp
/// <exception cref="SpecificException">
/// Thrown when [condition].
/// Returns HTTP [status code] with error code "[ERROR_CODE]".
/// </exception>
public async Task<Response> Handle(Command request, CancellationToken ct)
{
    if (somethingWrong)
        throw new SpecificException("message");
}
```

#### Scenario: Handler throws exception on validation failure

- **WHEN** a handler throws NotFoundException when resource not found
- **THEN** the exception is documented with HTTP 404 status code
- **AND** API consumers know what error response to expect
- **AND** Scalar UI displays the error scenario

### Requirement: Scalar UI Integration

Api.xml documentation SHALL be integrated with Scalar UI for API exploration.

**Specification:**
- Api.xml location known to Scalar configuration
- Summaries appear in Scalar operation descriptions
- Remarks appear in expanded documentation
- Parameter descriptions displayed
- Exception codes listed

#### Scenario: API consumer views handler in Scalar UI

- **WHEN** a handler with complete XML documentation is viewed in Scalar UI
- **THEN** all descriptions appear in the UI
- **AND** parameter details are visible
- **AND** error scenarios are documented

### Requirement: IDE IntelliSense Support

XML documentation SHALL appear in IDE tooltips and IntelliSense.

**Specification:**
- Hovering over handler → summary + remarks shown
- Hovering over parameter → description shown
- Hovering over method → return/exception info shown
- "Go to Definition" preserves documentation

#### Scenario: Developer browses code in IDE

- **WHEN** a developer opens a documented handler in IDE
- **THEN** IDE tooltip shows the summary and remarks on hover
- **AND** the developer understands the handler's purpose without reading code

### Requirement: Documentation Standards Compliance

All documentation SHALL follow consistent standards and patterns.

**Summary Guidelines:**
- One sentence describing what the API does
- Present tense, active voice
- Verb at start for methods
- Noun phrase at start for types

**Remarks Guidelines:**
- Business context and purpose
- State transitions if applicable
- Business rules and constraints
- Side effects and audit trail
- Error conditions and handling

**Exception Guidelines:**
- When exception is thrown
- HTTP status code
- Error code/identifier
- User-facing message

**Parameter Guidelines:**
- What the parameter represents
- Valid values or format
- Required vs optional
- Constraints or limits

#### Scenario: Code reviewer checks documentation standards

- **WHEN** a pull request with new handlers is reviewed
- **THEN** all comments follow the established standards
- **AND** the documentation is consistent across the codebase

## Dependencies

- None (XML documentation is built-in .NET feature)
- Requires Scalar.AspNetCore package (already in project)
