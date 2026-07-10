# Specification: Exception Handling Standardization

## Overview

This specification defines the standardized exception handling architecture for the Scaffold API. The architecture is universal - designed to work consistently across all domains and entities in the system, enabling consistent error responses and tracing regardless of which endpoint is called.

## ADDED Requirements

### Requirement: New Domain-Specific Exception Types
The system SHALL provide universal domain-specific exception types for common error scenarios that apply across all domains and entities.

The system MUST include the following new exception types:
- `NotFoundException` for any resource not found scenario (universal)
- `ConflictException` for any business rule conflict (duplicates, state violations)
- `UnauthorizedException` for business rule authorization failures (universal)
- `DomainException` as base class for extending with domain-specific exceptions

These exceptions are NOT specific to any single entity or domain - they apply universally to all resources and operations in the system.

#### Scenario: NotFoundException for Missing Resource
```
Given a request to retrieve a non-existent resource (any entity type)
When the service attempts to load the resource
Then a NotFoundException MUST be thrown with descriptive message
And the HTTP response status code SHALL be 404
```

#### Scenario: ConflictException for Business Rule Conflicts
```
Given a request that violates a business rule due to conflicting data
When business rule validation is performed
Then a ConflictException MUST be thrown with descriptive message
And the HTTP response status code SHALL be 409
```

#### Scenario: UnauthorizedException for Business Authorization
```
Given a request that violates a business rule authorization check
When business rule authorization is evaluated
Then an UnauthorizedException MUST be thrown
And the HTTP response status code SHALL be 403
```

### Requirement: Standardized Error Response Format
The system MUST return error responses in RFC 7807 Problem Details format.

All error responses SHALL include:
- `type`: URI identifying the error type
- `title`: Human-readable error type title
- `status`: HTTP status code
- `detail`: Detailed error message
- `instance`: The request path that caused the error
- `traceId`: Correlation ID for request tracking
- `errors`: (Optional) Dictionary of validation errors by field

#### Scenario: Validation Error Response Format
```
Given a request with invalid input
When the ValidationBehavior validates the request
Then the error response MUST include:
  - status: 422
  - title: "Validation Error"
  - errors: { "fieldName": ["error message"] }
  - traceId: correlation ID from request context
```

#### Scenario: Business Rule Error Response Format
```
Given a request violating a business rule
When the domain service rejects the operation
Then the error response MUST include:
  - status: 409 (for conflict) or 403 (for authorization)
  - title: error type name
  - detail: business rule violation message
  - traceId: correlation ID
  - errors: null (no field-level errors)
```

### Requirement: Correlation ID Tracking
The system MUST track all requests with correlation IDs for debugging and monitoring.

The system SHALL:
- Generate or extract correlation ID from request headers
- Include correlation ID in all error responses (as `traceId`)
- Use correlation ID in structured logging
- Make correlation ID available throughout request lifecycle

#### Scenario: Correlation ID in Error Response
```
Given a request with header X-Correlation-ID: test-123
When an exception occurs during processing
Then the error response traceId SHALL be "test-123"
And all logs for this request SHALL include correlation ID "test-123"
```

### Requirement: Exception to HTTP Status Code Mapping
The system MUST map all exception types to appropriate HTTP status codes.

The middleware SHALL map exceptions as follows:
- ValidationAppException → 422 Unprocessable Entity
- BusinessLogicException → 400 Bad Request
- NotFoundException → 404 Not Found
- ConflictException → 409 Conflict
- UnauthorizedException → 403 Forbidden
- DomainException → 422 Unprocessable Entity
- BadHttpRequestException → 400 Bad Request
- KeyNotFoundException → 404 Not Found
- All other exceptions → 500 Internal Server Error

#### Scenario: Correct Status Code for Each Exception Type
```
Given different exception types thrown in handlers
When the ExceptionHandlingMiddleware processes the exception
Then the HTTP response status code MUST match the mapping table above
```

## ADDED Requirements

### Requirement: Enhanced ExceptionHandlingMiddleware
The existing ExceptionHandlingMiddleware MUST be extended to support new exception types while maintaining backward compatibility.

The middleware SHALL:
- Continue to catch all exceptions
- Map all exception types (existing and new) correctly
- Include correlation ID in error response
- Maintain existing error response structure
- Log exceptions with proper severity levels

#### Scenario: Backward Compatibility with Existing Exceptions
```
Given existing code throwing BusinessLogicException
When the middleware processes the exception
Then the error response format SHALL remain unchanged
And the HTTP status code SHALL remain 400
And existing clients SHALL not require code changes
```

#### Scenario: Enhanced Logging with Correlation
```
Given an exception thrown during request processing
When the middleware logs the error
Then the log entry SHALL include:
  - Exception type and message
  - Correlation ID from request context
  - Request path and method
  - User context (if available)
```

### Requirement: Error Response Structure
The error response structure SHALL conform to RFC 7807 Problem Details standard.

All error responses SHALL include required fields:
- `type`: Error type identifier
- `title`: Human-readable error name
- `status`: HTTP status code
- `detail`: Error message
- `traceId`: Correlation ID

Optional fields for specific error types:
- `errors`: Field-level validation errors (for 422)
- `instance`: Request path

#### Scenario: Complete Error Response Structure
```
Given a validation error response
When the response is serialized as JSON
Then all required fields SHALL be present
And the JSON structure SHALL be parseable by RFC 7807 consumers
```

## Testing Requirements

### Unit Tests MUST cover:
- Each new exception type creates correctly
- Exception properties are accessible
- Exception messages are descriptive

### Integration Tests MUST cover:
- Each exception type results in correct HTTP status
- Error response format is RFC 7807 compliant
- Correlation IDs are present in responses
- Backward compatibility with existing exceptions

### Middleware Tests MUST cover:
- Unknown exceptions result in 500 status
- All mapped exceptions return correct status codes
- Error details are properly formatted
- Correlation ID is correctly extracted and included
