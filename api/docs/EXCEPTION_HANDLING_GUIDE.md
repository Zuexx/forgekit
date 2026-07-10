# Exception Handling Guide

## Overview

This guide documents the standardized exception handling patterns used throughout the ForgeKit API. It provides clear guidance on when and how to use each exception type, how exceptions are handled by the middleware, and how API consumers should interpret error responses.

The exception system is designed to be universal across all domains and entities, following proven patterns from Domain-Driven Design and REST API best practices.

## Table of Contents

1. [Exception Types](#exception-types)
2. [When to Use Each Exception](#when-to-use-each-exception)
3. [Usage Examples](#usage-examples)
4. [Error Response Format](#error-response-format)
5. [Correlation IDs](#correlation-ids)
6. [Troubleshooting](#troubleshooting)

## Exception Types

### DomainException (Base Class)
**HTTP Status:** 422 Unprocessable Entity

Base exception class for all domain-level business rule violations. Derived exceptions inherit from this class to provide more specific error information.

**Properties:**
- `Message`: Human-readable error description
- `Code`: Machine-readable error code (e.g., "NOT_FOUND", "CONFLICT")
- `Details`: Optional dictionary of field-level errors

**Usage:** Abstract class - use derived types instead.

### NotFoundException
**HTTP Status:** 404 Not Found

Thrown when a required resource cannot be found.

**When to use:**
- Any resource lookup returns null but is required
- User tries to access a non-existent resource
- Referenced entity in a relationship doesn't exist
- Resource has been deleted

**Applies to:** Any domain entity (Patient, Visit, Order, etc.)

**Constructor:**
```csharp
// Message, optional resource type
new NotFoundException(message, resourceType?)
```

**Example:**
```csharp
var resource = await repository.GetByIdAsync(id);
if (resource == null)
{
    throw new NotFoundException($"Resource with ID {id} not found", "Resource");
}
```

### ConflictException
**HTTP Status:** 409 Conflict

Thrown when an operation violates business rules due to conflicting data or state.

**When to use:**
- Attempting to create a duplicate record (email, identifier, etc.)
- Resource is in an invalid state for the requested operation
- Unique constraint violation
- Business rule prevents operation due to existing data

**Applies to:** Any domain with uniqueness constraints or state-based operations

**Constructor:**
```csharp
// Message, optional conflict field name
new ConflictException(message, conflictField?)
```

**Example:**
```csharp
var existing = await repository.FindByEmailAsync(newEmail);
if (existing != null)
{
    throw new ConflictException("Email already registered", "email");
}
```

### UnauthorizedException
**HTTP Status:** 403 Forbidden

Thrown when a business rule denies authorization for an operation. This is distinct from authentication failures (no valid token).

**When to use:**
- User lacks required permissions for an operation
- Business rules prevent the operation (state-based restrictions)
- Resource access is denied due to ownership or role
- Business constraint violation

**Applies to:** Any operation with business rule authorization checks

**Note:** For authentication failures (no token), the framework handles 401 Unauthorized.
Use UnauthorizedException for business-level authorization checks.

**Example:**
```csharp
if (!user.HasPermission("cancel_visit"))
{
    throw new UnauthorizedException("You lack permission to cancel visits");
}

if (visit.IsInPast())
{
    throw new UnauthorizedException("Cannot cancel a visit that has already occurred");
}
```

### BusinessLogicException
**HTTP Status:** 400 Bad Request

Legacy exception for general business logic violations. Maintained for backward compatibility.

**When to use:**
- General business rule violations not covered by above types
- Backward compatibility with existing code

**Note:** Consider using more specific exception types (NotFoundException, ConflictException, UnauthorizedException) for new code.

### ValidationAppException
**HTTP Status:** 422 Unprocessable Entity

Thrown by validation pipeline for input validation failures. Contains field-level error details.

**Properties:**
- `Errors`: Dictionary mapping field names to error message arrays

**When to use:** 
- Automatically thrown by FluentValidation
- Manual validation errors with field-level details

**Example:**
```csharp
var errors = new Dictionary<string, string[]>
{
    { "email", new[] { "Invalid email format" } },
    { "age", new[] { "Must be at least 18" } }
};
throw new ValidationAppException(errors);
```

## When to Use Each Exception

### By Layer

**API Controller/Handler:**
- Handle ValidationException and convert to ValidationAppException with field errors
- Let domain exceptions propagate to middleware

**Domain Service/Business Logic:**
- Throw domain exceptions for business rule violations
- Throw NotFoundException when entities don't exist
- Throw ConflictException for conflicts with existing data
- Throw UnauthorizedException for permission denials

**Repository:**
- Throw NotFoundException when entity lookup returns null
- Let EF Core exceptions bubble up (will be caught by middleware)

### By Scenario

These scenarios apply across all domains:

| Scenario | Exception | Status | Notes |
|----------|-----------|--------|-------|
| Resource missing/deleted | NotFoundException | 404 | Universal - applies to any entity |
| Duplicate/conflict detected | ConflictException | 409 | Any uniqueness constraint or state conflict |
| Business rule authorization denied | UnauthorizedException | 403 | State-based or permission-based rules |
| Invalid input data | ValidationAppException | 422 | Validation pipeline automatically throws |
| Other domain violations | DomainException | 422 | Subclass for custom domain rules |
| General business logic error | BusinessLogicException | 400 | Legacy - use more specific types for new code |
| Unhandled errors | (caught by middleware) | 500 | Framework/infrastructure errors |

## Usage Examples

### Example 1: Create Patient with Validation
```csharp
[HttpPost]
public async Task<ActionResult> CreatePatient(CreatePatientRequest request)
{
    // Validation happens automatically via pipeline
    // If invalid, ValidationAppException is thrown with field errors
    
    // Check for duplicate email
    var existing = await _repository.FindByEmailAsync(request.Email);
    if (existing != null)
    {
        throw new ConflictException(
            "Patient with this email already exists", 
            "email"
        );
    }
    
    var patient = new Patient { Email = request.Email, ... };
    await _repository.AddAsync(patient);
    return CreatedAtAction(nameof(GetPatient), patient);
}
```

### Example 2: Update Visit with Authorization Check
```csharp
[HttpPut("{id}")]
public async Task<ActionResult> UpdateVisit(int id, UpdateTodoStatusRequest request)
{
    var visit = await _repository.GetVisitAsync(id);
    
    // Not found
    if (visit == null)
    {
        throw new NotFoundException(
            $"Visit with ID {id} not found",
            "Visit"
        );
    }
    
    // Authorization - business rule check
    if (visit.Date < DateTime.Today)
    {
        throw new UnauthorizedException(
            "Cannot modify a visit that has already occurred"
        );
    }
    
    visit.Update(request);
    await _repository.SaveAsync();
    return Ok(visit);
}
```

### Example 3: Custom Domain Exception
```csharp
public class InsufficientFundsException : DomainException
{
    public decimal Required { get; }
    public decimal Available { get; }
    
    public InsufficientFundsException(decimal required, decimal available)
        : base($"Insufficient funds. Required: {required}, Available: {available}", "INSUFFICIENT_FUNDS")
    {
        Required = required;
        Available = available;
    }
}

// Usage
if (account.Balance < amount)
{
    throw new InsufficientFundsException(amount, account.Balance);
}
```

## Error Response Format

All errors are returned in **RFC 7807 Problem Details** format:

```json
{
  "title": "Validation Error",
  "status": 422,
  "detail": "One or more validation errors occurred",
  "traceId": "0HN8KHDG7H9L0:00000001",
  "errors": {
    "email": ["Invalid email format"],
    "age": ["Must be at least 18"]
  }
}
```

### Response Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| title | string | Yes | Human-readable error type (e.g., "Not Found", "Conflict") |
| status | number | Yes | HTTP status code |
| detail | string | Yes | Specific error message for this occurrence |
| traceId | string | Yes | Unique correlation ID for request tracing |
| errors | object | No | Field-level error details (validation/domain errors) |

### Error Response Examples

**404 Not Found:**
```json
{
  "title": "Not Found",
  "status": 404,
  "detail": "Patient with ID 999 not found",
  "traceId": "0HN8KHDG7H9L0:00000001"
}
```

**409 Conflict:**
```json
{
  "title": "Conflict",
  "status": 409,
  "detail": "Email already registered",
  "traceId": "0HN8KHDG7H9L0:00000002",
  "errors": {
    "email": ["This email is already in use"]
  }
}
```

**403 Forbidden:**
```json
{
  "title": "Forbidden",
  "status": 403,
  "detail": "Cannot cancel a visit that has already occurred",
  "traceId": "0HN8KHDG7H9L0:00000003"
}
```

**422 Validation Error:**
```json
{
  "title": "Validation Error",
  "status": 422,
  "detail": "One or more validation errors occurred",
  "traceId": "0HN8KHDG7H9L0:00000004",
  "errors": {
    "email": ["Invalid email format"],
    "dateOfBirth": ["Patient must be at least 18 years old"]
  }
}
```

## Correlation IDs

### What is a Correlation ID?

A correlation ID (trace ID) is a unique identifier generated for each request. It's included in error responses and logs, allowing you to trace related events across the system.

### How It Works

1. **Extraction:** The middleware looks for a `X-Correlation-ID` header in the request
2. **Generation:** If not provided, the system generates one (using HttpContext.TraceIdentifier)
3. **Inclusion:** The ID is included in the error response's `traceId` field
4. **Logging:** The ID is logged with structured logging for request tracing

### Using Correlation IDs

**When sending requests:**
```http
GET /api/patients/999
X-Correlation-ID: client-request-123
```

**In error response:**
```json
{
  "title": "Not Found",
  "status": 404,
  "detail": "Patient not found",
  "traceId": "client-request-123"
}
```

**In logs:**
```
[ERROR] Unhandled exception with correlation ID client-request-123: Patient not found
```

### Troubleshooting with Correlation IDs

1. Save the `traceId` from the error response
2. Use it to search logs: `grep "client-request-123" app.log`
3. View all related operations for that request
4. Share with support team for investigation

## Troubleshooting

### Common Issues

**Issue: Getting 500 error instead of specific error**
- Check that you're throwing the correct exception type
- Verify middleware is registered in Program.cs
- Check application logs for the actual exception

**Issue: Field errors not included in response**
- Ensure you're using ValidationAppException with proper error dictionary
- For domain exceptions, set the `Details` property

**Issue: Correlation ID not matching logs**
- Verify client is setting `X-Correlation-ID` header correctly
- Check if system-generated ID is being used instead
- Log includes correlation ID in error message

**Issue: Getting 400 instead of 404 for missing resource**
- Ensure you're throwing NotFoundException, not BadHttpRequestException
- Check repository method returns null (not throwing)

### Debugging Tips

1. **Enable verbose logging** to see correlation ID in logs
2. **Use browser dev tools** to inspect request/response headers
3. **Save error responses** with traceId for investigation
4. **Test with Postman/curl** to verify response format
5. **Check middleware registration** in Program.cs

### For API Consumers

When reporting errors:
- Include the `traceId` from the error response
- Include the request you made
- Include the exact error response JSON
- Note the timestamp of the error

This information helps developers diagnose issues quickly.
