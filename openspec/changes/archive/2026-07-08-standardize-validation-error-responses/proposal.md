# Proposal: Validation Error Response Standardization

**Created:** 2026-02-09  
**Status:** Proposed  
**Priority:** Medium  
**Effort:** Low (4-6 hours)  

## Overview

Standardize API validation error responses to provide consistent, predictable error information across all endpoints. Currently, `ValidationAppException` exists but lacks a standardized DTO structure and comprehensive error metadata.

## Why

**Problem:** API clients cannot reliably handle different error types because:
- Error response format varies across endpoints
- No machine-readable error codes for programmatic handling
- Missing trace IDs make debugging difficult
- Field-level validation errors not standardized
- Clients must parse different error structures

**Impact:** Harder error handling in client applications, poor user experience with validation feedback, difficult debugging in production

**Solution:** Standardize all error responses with consistent DTO structure, error codes, and trace IDs

## Problem Statement

### Current State
- ✅ `ValidationAppException` exists for validation errors
- ✅ `ExceptionHandlingMiddleware` processes exceptions
- ❌ Error response structure is inconsistent
- ❌ Missing error codes for programmatic handling
- ❌ No trace ID for error tracking
- ❌ Field-level validation errors not standardized

### Example Current Response
```json
{
  "message": "Validation failed",
  "errors": {
    "dueDate": ["Must be a future date"],
    "name": ["Cannot be empty"]
  }
}
```

### Desired Standardized Response
```json
{
  "message": "Validation failed",
  "code": "VALIDATION_ERROR",
  "timestamp": "2026-02-09T12:00:00Z",
  "traceId": "0HN1GG2P6JFBM:00000001",
  "errors": {
    "dueDate": ["Must be a future date"],
    "name": ["Cannot be empty"]
  }
}
```

## Solution Overview

### 1. Standardized Error Response DTO
Create a unified `ErrorResponse` class that all exception handlers return consistently.

### 2. Error Codes
Define error codes for programmatic client handling:
- `VALIDATION_ERROR` (422)
- `RESOURCE_NOT_FOUND` (404)
- `CONFLICT_ERROR` (409)
- `UNAUTHORIZED_ERROR` (403)
- `INTERNAL_SERVER_ERROR` (500)

### 3. Enhanced ExceptionHandlingMiddleware
Update middleware to populate all fields:
- Message
- Code
- Timestamp
- TraceId (from HttpContext)
- Errors (for validation exceptions)

### 4. ValidationBehavior Integration
Ensure `ValidationBehavior` in pipeline populates error structure consistently.

## Success Criteria

- ✅ `ErrorResponse` DTO created with all required fields
- ✅ Error codes standardized across all exception types
- ✅ ExceptionHandlingMiddleware returns consistent structure
- ✅ All validation errors include field-level details
- ✅ All 134 tests passing (no regression)
- ✅ API responses documented with example error schemas

## Implementation Phases

| Phase | Title | Effort | Duration |
|-------|-------|--------|----------|
| 1 | Create ErrorResponse DTO and error codes | 1 hour | 1 day |
| 2 | Update ExceptionHandlingMiddleware | 1.5 hours | 1 day |
| 3 | Verify ValidationBehavior integration | 1 hour | 1 day |
| 4 | Testing and validation | 1.5 hours | 1 day |

## Risk Assessment

- **Low Risk:** Changes are isolated to exception handling
- **Mitigation:** All changes backwards-compatible with existing exception types
- **Testing:** Middleware tests verify consistent response format

## Rollback Plan

If issues discovered:
1. Revert `ErrorResponse` changes
2. Restore previous middleware implementation
3. Re-run test suite to verify

## Notes

- Will be implemented on separate branch: `feature/standardize-validation-responses`
- Does not affect domain layer or service logic
- Improves API consistency for client error handling
- Foundation for future error tracking/monitoring integration
