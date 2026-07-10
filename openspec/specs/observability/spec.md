# observability Specification

## Purpose
Defines structured logging, correlation IDs, log levels, and operational observability expectations.
## Requirements
### Requirement: Structured Logging Framework
The system SHALL provide structured logging throughout the application using Serilog, with output to both console and rolling file sinks.

#### Scenario: Serilog configured on startup
- **WHEN** the application starts
- **THEN** Serilog is configured as the logging provider with console and file sinks

#### Scenario: Log files roll daily
- **WHEN** a new day begins
- **THEN** a new log file is created with the current date in the filename

#### Scenario: Minimum log level respects environment
- **WHEN** the application runs in Development
- **THEN** the minimum log level is Debug
- **WHEN** the application runs in Production
- **THEN** the minimum log level is Information

---

### Requirement: Request Correlation Tracking
The system SHALL track request flows using correlation IDs injected into the log context.

#### Scenario: Correlation ID generated per request
- **WHEN** an HTTP request arrives
- **THEN** a unique correlation ID is generated or extracted from the request header
- **AND** the correlation ID is added to the logging context for all downstream logs

#### Scenario: Correlation ID available in logs
- **WHEN** any log is written during request processing
- **THEN** the correlation ID is included as a structured property in the log

#### Scenario: Correlation ID propagates through async operations
- **WHEN** async operations execute within a request scope
- **THEN** the correlation ID remains available in the log context

---

### Requirement: Handler and Service Logging
The system SHALL emit structured logs at key points in handler and service execution.

#### Scenario: Handler logs on entry and success
- **WHEN** a MediatR handler processes a request
- **THEN** it logs the handler name and key input parameters on entry
- **AND** it logs the result on successful completion

#### Scenario: Handler logs on failure
- **WHEN** a MediatR handler throws an exception
- **THEN** it logs the exception with context (handler name, request parameters, error details)

#### Scenario: Service logs business operations
- **WHEN** a service performs a business operation (create, update, delete, state change)
- **THEN** it logs the operation type, affected entities, and the user performing the action

#### Scenario: Domain service logs validation and rules
- **WHEN** a domain service applies business rules or validates state
- **THEN** it logs rule checks and validation outcomes for audit trail purposes

---

### Requirement: Structured Log Enrichment
The system SHALL enrich logs with contextual metadata about the application and request.

#### Scenario: Application name in all logs
- **WHEN** any log is written
- **THEN** it includes the property "Application" with value "ForgeKit.Api"

#### Scenario: Timestamp standardization
- **WHEN** a log is written
- **THEN** it includes an ISO 8601 timestamp in UTC

#### Scenario: User context in logs
- **WHEN** a request is processed by an authenticated handler
- **THEN** the log includes user identity information (user ID or claim value)

---

### Requirement: Exception Logging Integration
The system SHALL log exceptions with full context and stack traces, coordinated with the exception handling middleware.

#### Scenario: Unhandled exceptions logged before middleware handles them
- **WHEN** an exception propagates to the exception handling middleware
- **THEN** it is logged with full exception details, stack trace, and correlation ID
- **AND** the middleware returns a structured error response to the client

#### Scenario: Sensitive information protected in logs
- **WHEN** logging occurs
- **THEN** sensitive data (passwords, tokens, PII) is not included in logs

---

### Requirement: Log Level Usage
The system SHALL use consistent log levels across the application.

#### Scenario: Debug level for detailed diagnostics
- **WHEN** detailed diagnostic information is needed
- **THEN** Debug level is used (e.g., parameter values, internal state transitions)

#### Scenario: Information level for business events
- **WHEN** important business events occur (entity created, status changed, action taken)
- **THEN** Information level is used

#### Scenario: Warning level for recoverable issues
- **WHEN** a potentially problematic condition is detected but handled
- **THEN** Warning level is used (e.g., retry attempt, deprecated API usage)

#### Scenario: Error level for failures
- **WHEN** an operation fails but the application continues
- **THEN** Error level is used

#### Scenario: Fatal level for critical failures
- **WHEN** a failure prevents the application from continuing
- **THEN** Fatal level is used

---

### Requirement: Configurable Logging Behavior
The system SHALL allow configuration of logging behavior via application settings.

#### Scenario: Log level configurable per namespace
- **WHEN** the application reads configuration
- **THEN** it allows override of log levels per namespace (e.g., set Microsoft.* to Warning)

#### Scenario: Log output path configurable
- **WHEN** the application reads configuration
- **THEN** it allows customization of the log file path and retention policy

#### Scenario: Structured property enrichment configurable
- **WHEN** the application starts
- **THEN** additional custom properties can be added to all logs via configuration
