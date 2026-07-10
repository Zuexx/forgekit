# error-response Specification

## Purpose
Defines the standardized ErrorResponse contract, machine-readable error codes, and middleware mapping for API failures.
## Requirements
### Requirement: ErrorResponse DTO

The API SHALL return a standardized `ErrorResponse` object for all error scenarios, providing consistent error information to clients including message, error code, timestamp, trace ID, and field-level validation errors.

**Acceptance Criteria:**
- ErrorResponse class MUST be located in ForgeKit.Api/Models/ErrorResponse.cs
- ErrorResponse MUST include Message property (string)
- ErrorResponse MUST include Code property (string?)
- ErrorResponse MUST include Timestamp property (DateTime)
- ErrorResponse MUST include TraceId property (string?)
- ErrorResponse MUST include Errors property (Dictionary<string, string[]>?)
- ErrorResponse MUST be serializable to JSON

#### Scenario: Validation Error Response with field-level errors
- **WHEN** a request is posted with invalid data
- **THEN** the response is 422 Unprocessable Entity
- **THEN** the response includes code "VALIDATION_ERROR"
- **THEN** the response includes timestamp in UTC
- **THEN** the response includes traceId from HttpContext
- **THEN** the response includes errors dict with field names as keys
- **THEN** each field error is an array of error messages

#### Scenario: Not Found Error Response
- **WHEN** a GET request for non-existent resource is made
- **THEN** the response is 404 Not Found
- **THEN** the response includes code "RESOURCE_NOT_FOUND"
- **THEN** the response includes timestamp in UTC
- **THEN** the response includes traceId for debugging
- **THEN** the response does NOT include errors field

### Requirement: Error Code Constants

The system SHALL define machine-readable error codes for programmatic client error handling.

**Acceptance Criteria:**
- ErrorCodes class MUST be located in ForgeKit.Api/Constants/ErrorCodes.cs
- MUST define VALIDATION_ERROR constant (422)
- MUST define RESOURCE_NOT_FOUND constant (404)
- MUST define CONFLICT_ERROR constant (409)
- MUST define UNAUTHORIZED_ERROR constant (403)
- MUST define BUSINESS_LOGIC_ERROR constant (400)
- MUST define INVALID_STATE_ERROR constant (400)
- MUST define INTERNAL_SERVER_ERROR constant (500)

#### Scenario: Validation Error Code
- **WHEN** ValidationAppException is thrown
- **THEN** error code MUST be "VALIDATION_ERROR"

#### Scenario: Not Found Error Code
- **WHEN** NotFoundException is thrown
- **THEN** error code MUST be "RESOURCE_NOT_FOUND"
- **THEN** HTTP status MUST be 404

#### Scenario: Business Logic Error Code
- **WHEN** BusinessLogicException is thrown
- **THEN** error code MUST be "BUSINESS_LOGIC_ERROR"
- **THEN** HTTP status MUST be 400

### Requirement: ExceptionHandlingMiddleware

The exception handling middleware SHALL return ErrorResponse objects with all required fields properly populated based on exception type.

**Acceptance Criteria:**
- Middleware MUST catch all exceptions and return ErrorResponse
- Middleware MUST populate Message from exception.Message
- Middleware MUST populate Code based on exception type
- Middleware MUST populate Timestamp with DateTime.UtcNow
- Middleware MUST populate TraceId from context.TraceIdentifier
- Middleware MUST populate Errors for ValidationAppException
- Middleware MUST set correct HTTP status code per exception type
- Middleware MUST map ValidationAppException to 422 Unprocessable Entity
- Middleware MUST map NotFoundException to 404 Not Found
- Middleware MUST map ConflictException to 409 Conflict
- Middleware MUST map UnauthorizedException to 403 Forbidden
- Middleware MUST map BusinessLogicException to 400 Bad Request
- Middleware MUST map InvalidStateException to 400 Bad Request
- Middleware MUST map unhandled exceptions to 500 Internal Server Error

#### Scenario: Validation Error Response
- **WHEN** a ValidationAppException is caught in middleware
- **AND** the exception has field-level errors
- **THEN** the response code is 422
- **THEN** the response code field is "VALIDATION_ERROR"
- **THEN** the response includes errors dict with field names and messages
- **THEN** the response includes timestamp and traceId

#### Scenario: Resource Not Found Response
- **WHEN** a NotFoundException is caught in middleware
- **THEN** the response code is 404
- **THEN** the response code field is "RESOURCE_NOT_FOUND"
- **THEN** the response does NOT include errors field
- **THEN** the response includes timestamp and traceId

#### Scenario: Business Logic Error Response
- **WHEN** a BusinessLogicException is caught in middleware
- **AND** the exception message is about restore grace period
- **THEN** the response code is 400
- **THEN** the response code field is "BUSINESS_LOGIC_ERROR"
- **THEN** the response message includes the business rule violation details
- **THEN** the response includes timestamp and traceId

#### Scenario: Conflict Error Response
- **WHEN** a ConflictException is caught in middleware
- **THEN** the response code is 409
- **THEN** the response code field is "CONFLICT_ERROR"
- **THEN** the response includes timestamp and traceId

#### Scenario: TraceId Correlation
- **WHEN** an error occurs in any handler
- **THEN** the response includes a traceId
- **AND** the traceId matches the HttpContext.TraceIdentifier
- **AND** support team searches logs with this traceId
- **THEN** all log entries for this request can be found
