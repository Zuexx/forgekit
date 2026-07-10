# Architecture Decision Record: Exception Handling Standardization

## Status
ACCEPTED

## Date
2026-02-04

## Context

The ForgeKit API requires a standardized approach to exception handling that works across all domains and entities in the system. Basic exception handling existed through middleware and validation behavior, but lacked:

1. **Standardized exception hierarchy** - Different exception types for different error scenarios, applicable across all domains
2. **Consistent error response format** - Clients need to rely on a standard structure regardless of which endpoint they call
3. **Request tracing capability** - Correlation IDs enable debugging across the system and in distributed deployments
4. **Clear categorization** - Exceptions should be easily distinguished: client vs server errors, validation vs business logic
5. **Extensible architecture** - Adding new domain exceptions should be straightforward and consistent

This lack of standards led to:
- Inconsistent error responses across different endpoints and domains
- Difficulty debugging issues in production (no correlation IDs)
- Confusion about appropriate HTTP status codes for different scenarios
- No structured logging context to tie logs together

## Decision

We have standardized exception handling with the following architecture:

### 1. Exception Hierarchy

```
Exception
├── DomainException (NEW)
│   ├── NotFoundException (NEW)
│   ├── ConflictException (NEW)
│   ├── UnauthorizedException (NEW)
│   └── [Custom domain exceptions for any domain]
├── ValidationAppException (EXISTING)
└── BusinessLogicException (EXISTING - legacy)
```

Note: All new exceptions inherit from DomainException, creating a clear, extensible hierarchy
for adding domain-specific exceptions across any part of the system.

### 2. HTTP Status Code Mapping

| Exception Type | HTTP Status | Use Case |
|---|---|---|
| NotFoundException | 404 | Resource not found |
| ConflictException | 409 | Business rule conflict (duplicate, state mismatch) |
| UnauthorizedException | 403 | Permission denied (business rule) |
| DomainException | 422 | General domain business rule violations |
| ValidationAppException | 422 | Input validation failures |
| BusinessLogicException | 400 | General business logic errors (legacy) |
| Unhandled exceptions | 500 | Unexpected server errors |

### 3. Error Response Format

RFC 7807 Problem Details format with correlation ID:

```json
{
  "title": "Error Type",
  "status": 400,
  "detail": "Specific error message",
  "traceId": "correlation-id-123",
  "errors": { "field": ["error messages"] }
}
```

### 4. Correlation ID Tracking

- Extract from `X-Correlation-ID` header
- Generate `context.TraceIdentifier` if not provided
- Include in all error responses
- Log with structured logging for debugging

### 5. Middleware Enhancement

Enhanced `ExceptionHandlingMiddleware` to:
- Map all exception types to HTTP status codes
- Extract and store correlation IDs
- Format error responses as ErrorResponse DTO
- Include field-level error details

## Consequences

### Positive

1. **Universal Consistency** - All errors follow the same format across the entire API, regardless of domain or entity
2. **Tracability** - Correlation IDs enable request tracing across the system and in distributed deployments
3. **Clarity** - Clear, standard HTTP status codes for each error category make API contracts predictable
4. **Extensibility** - New domain exceptions inherit from DomainException, maintaining consistency as the system grows
5. **Backward Compatibility** - Existing exceptions continue to work without modification
6. **Standards Compliance** - RFC 7807 compliance ensures compatibility with industry-standard tooling
7. **Developer Experience** - Clear, documented patterns make it easy for developers to throw appropriate exceptions
8. **Operations** - Correlation IDs and standardized format enable better monitoring and troubleshooting

### Neutral

1. **Learning Curve** - Developers new to the codebase need to understand when to use each exception type
2. **Migration Path** - Existing code could gradually migrate to new exception types (not required)

### Negative

1. **Code Changes** - Middleware requires updates to exception handlers
2. **Testing** - More test coverage required for all exception types

## Implementation

### Completed

- ✅ Created 4 new exception types (NotFoundException, ConflictException, UnauthorizedException, DomainException)
- ✅ Enhanced ExceptionHandlingMiddleware with correlation ID support
- ✅ Created ErrorResponse DTO for standardized serialization
- ✅ Added 39 tests (15 unit + 15 middleware + 9 integration)
- ✅ Verified RFC 7807 compliance
- ✅ Created comprehensive documentation

### Guidelines for All New Code

This standardized exception handling architecture is designed to work across all domains in the system.
When extending the API with new features or domains:

1. **Throw appropriate domain exceptions** from your business logic
   - Use the standard exceptions (NotFoundException, ConflictException, UnauthorizedException) when applicable
   - Create custom exceptions by extending DomainException for domain-specific errors

2. **Follow layer responsibilities:**
   - **Domain/Application Layer**: Business logic and domain exceptions
   - **Handlers/Controllers**: Orchestration, let exceptions propagate to middleware
   - **Repository/Data Layer**: Return null (don't throw) for not found queries
   - **Middleware**: Global exception handling and standardization

3. **Document and test your exception types**
   - Include usage examples in domain documentation
   - Add integration tests to verify correct HTTP status codes
   - Verify correlation ID presence and correctness

4. **Leverage correlation IDs in production**
   - Use TraceId to correlate logs across services
   - Include correlation ID when reporting issues to operations team
   - Configure centralized logging to index by correlation ID

5. **Extend for new domains**
   - Create domain-specific exception types by extending DomainException
   - Map new exceptions in middleware's GetStatusCode() if they require custom HTTP status
   - Follow the same patterns as NotFoundException, ConflictException, UnauthorizedException

## Alternatives Considered

### 1. Return Result<T> instead of Exceptions
**Rejected because:**
- Exceptions are idiomatic C# for error handling
- Would require major refactoring
- Middleware already implements exception handling
- Less familiar to team

### 2. Use ProblemDetails directly in handlers
**Rejected because:**
- Centralizes handling in middleware is cleaner
- Keeps handlers focused on business logic
- Reduces code duplication

### 3. Custom correlation ID format
**Rejected because:**
- HTTP standard header naming is clearer
- X-Correlation-ID is industry standard
- TraceIdentifier integration is simpler

## Related Documents

- [Exception Handling Guide](./EXCEPTION_HANDLING_GUIDE.md)
- [API Error Documentation](./API_ERRORS.md)
- [Implementation PR](#)

## References

- [RFC 7807 - Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807)
- [C# Exception Handling Best Practices](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/exceptions/)
- [HTTP Status Codes](https://tools.ietf.org/html/rfc7231#section-6)
