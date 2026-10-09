## MODIFIED Requirements

### Requirement: ErrorResponse DTO

The API SHALL return a standardized `ErrorResponse` object for all error scenarios, providing consistent error information to clients including message, error code, timestamp, trace ID, and field-level validation errors.

**Acceptance Criteria:**
- ErrorResponse class MUST be located in Anvil/Models/ErrorResponse.cs
- ErrorResponse MUST include Message property (string)
- ErrorResponse MUST include Code property (string?)
- ErrorResponse MUST include Timestamp property (DateTime)
- ErrorResponse MUST include TraceId property (string?)
- ErrorResponse MUST include Errors property (Dictionary<string, string[]>?)
- ErrorResponse MUST be serializable to JSON
- The serialized JSON MUST use camelCase property names (`message`, `code`, `timestamp`, `traceId`, `errors`, `title`, `status`, `detail`), as documented in `docs/api/API_ERRORS.md`

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

#### Scenario: Property names are camelCase on the wire
- **WHEN** any error response body is read as raw JSON, without case-insensitive matching
- **THEN** it has the properties `message`, `code`, `timestamp`, `traceId`, `title`, `status`, and `detail`
- **AND** it has no property named `Message` or `Code`

### Requirement: ExceptionHandlingMiddleware

The exception handling middleware SHALL return ErrorResponse objects with all required fields properly populated based on exception type.

**Acceptance Criteria:**
- Middleware MUST catch all exceptions and return ErrorResponse
- Middleware MUST populate Message and Detail from exception.Message for every exception type it maps to a status other than 500
- Middleware MUST populate Message and Detail with a fixed generic message for exceptions it maps to 500 Internal Server Error, and MUST NOT include any part of the exception's message, type, or stack trace in the response
- Middleware MUST log the original exception, including its message, for every exception it handles
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

#### Scenario: Unhandled exception does not leak its message
- **WHEN** an exception with no explicit mapping (for example an InvalidOperationException whose message contains a connection string) is caught in middleware
- **THEN** the response code is 500
- **THEN** the response code field is "INTERNAL_SERVER_ERROR"
- **THEN** the response message and detail are the fixed generic message
- **THEN** neither field contains any part of the exception's message
- **AND** the logged entry contains the exception and its original message

#### Scenario: TraceId Correlation
- **WHEN** an error occurs in any handler
- **THEN** the response includes a traceId
- **AND** the traceId matches the HttpContext.TraceIdentifier
- **AND** support team searches logs with this traceId
- **THEN** all log entries for this request can be found
