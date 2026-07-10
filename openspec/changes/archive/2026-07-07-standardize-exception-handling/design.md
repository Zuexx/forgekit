# Design: Exception Handling Standardization

## Architecture

### Exception Hierarchy

```
Exception (Base)
├── ApplicationException (Custom Base)
│   ├── ValidationAppException (Existing)
│   ├── BusinessLogicException (Existing)
│   ├── NotFoundException (NEW)
│   ├── ConflictException (NEW)
│   ├── UnauthorizedException (NEW - for business rules)
│   └── DomainException (NEW - general base for domain errors)
└── Framework Exceptions
    ├── BadHttpRequestException
    ├── KeyNotFoundException
    └── FluentValidation.ValidationException
```

### Exception to HTTP Status Code Mapping

| Exception Type | HTTP Status | Use Case |
|---|---|---|
| ValidationAppException | 422 Unprocessable Entity | Input validation failures |
| BusinessLogicException | 400 Bad Request | General business rule violations |
| NotFoundException | 404 Not Found | Resource does not exist |
| ConflictException | 409 Conflict | Business rule conflicts (duplicate, state mismatch) |
| UnauthorizedException | 403 Forbidden | Business rule authorization failures |
| DomainException | 422 Unprocessable Entity | Domain-specific errors |
| BadHttpRequestException | 400 Bad Request | Invalid HTTP request |
| KeyNotFoundException | 404 Not Found | Resource not found |
| Other (unhandled) | 500 Internal Server Error | Unexpected errors |

### Error Response Format (RFC 7807 Problem Details)

```json
{
  "type": "https://api.scaffold.example/docs/errors/validation",
  "title": "Validation Error",
  "status": 422,
  "detail": "One or more validation errors occurred",
  "instance": "/api/todo-items",
  "traceId": "0HN8KHDG7H9L0:00000001",
  "errors": {
    "visitDate": ["Visit date must be at least 5 days in advance"],
    "mrId": ["Medical Representative not found"]
  }
}
```

### Implementation Details

#### 1. New Exception Types

**NotFoundException.cs**
- Thrown when a required resource does not exist
- Maps to HTTP 404
- Used in repositories/domain services when entity lookup fails

**ConflictException.cs**
- Thrown when business rule violation creates conflict
- Maps to HTTP 409
- Used for duplicate entries, state violations

**UnauthorizedException.cs**
- Thrown when business rule denies operation
- Maps to HTTP 403
- Different from AuthenticationException (no token)
- Example: "Cannot cancel visit for past date"

**DomainException.cs**
- Base exception for all domain-specific errors
- Maps to HTTP 422
- Allows domain layer to express business constraint violations

#### 2. Enhanced ExceptionHandlingMiddleware

Extend existing middleware to:
- Add correlation ID to request context
- Map all new exception types
- Preserve existing behavior (zero breaking changes)
- Log with structured context

#### 3. Error Response DTO

Create `ErrorResponse.cs` for standardized serialization:
- type: error type URI
- title: human-readable title
- status: HTTP status code
- detail: detailed message
- traceId: correlation ID
- errors: validation error dictionary

## Implementation Approach

### Phase 1: Add Exception Types
- Create new exception classes in `Api/Exceptions/`
- Add unit tests for each exception

### Phase 2: Enhance Middleware
- Update `ExceptionHandlingMiddleware` to handle new exceptions
- Add correlation ID tracking
- Ensure RFC 7807 compliance

### Phase 3: Create Error Response DTO
- Define `ErrorResponse` model
- Update middleware to use DTO

### Phase 4: Documentation & Integration Tests
- Document exception handling patterns
- Add integration tests for error scenarios
- Update API documentation

## Breaking Changes
**None** - All changes are additive. Existing exceptions and middleware behavior are preserved.

## Testing Strategy

1. **Unit Tests**
   - Each exception type has basic creation test
   - Exceptions serialize/deserialize correctly

2. **Integration Tests**
   - Each exception type triggers correct HTTP status code
   - Error response format is RFC 7807 compliant
   - Correlation IDs are included in responses

3. **Middleware Tests**
   - Unknown exceptions map to 500
   - Custom exceptions map to expected status codes
   - Error details are properly formatted

## Migration Path

Existing code requires no changes:
- `BusinessLogicException` continues to work (backward compatible)
- `ValidationAppException` continues to work (backward compatible)
- New exceptions are used in new features

## Monitoring & Logging

- Correlation IDs enable tracing across distributed systems
- Structured logging captures exception type and context
- Error aggregation tools can track patterns
