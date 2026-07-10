# Tasks: Standardize Validation Error Response

## Phase 1: Create Core Components (Effort: 1 hour)

- [x] Create `Api/Models/ErrorResponse.cs`
  - [x] Add Message property (string)
  - [x] Add Code property (string?)
  - [x] Add Timestamp property (DateTime)
  - [x] Add TraceId property (string?)
  - [x] Add Errors property (Dictionary<string, string[]>?)
  - [x] Add XML documentation to all properties

- [x] Create `Api/Constants/ErrorCodes.cs`
  - [x] Define ValidationError constant
  - [x] Define ResourceNotFound constant
  - [x] Define ConflictError constant
  - [x] Define UnauthorizedError constant
  - [x] Define InternalServerError constant
  - [x] Define BusinessLogicError constant
  - [x] Define InvalidStateError constant

## Phase 2: Update Middleware (Effort: 1.5 hours)

- [x] Update `Api/Middlewares/ExceptionHandlingMiddleware.cs`
  - [x] Import ErrorCodes constant
  - [x] Import ErrorResponse model
  - [x] Update HandleExceptionAsync to return ErrorResponse
  - [x] Add case for ValidationAppException with error codes
  - [x] Add case for NotFoundException
  - [x] Add case for ConflictException
  - [x] Add case for UnauthorizedException
  - [x] Add case for BusinessLogicException
  - [x] Add case for InvalidStateException
  - [x] Add case for default (500 error)
  - [x] Populate Timestamp field
  - [x] Populate TraceId from HttpContext
  - [x] Populate Code field based on exception type
  - [x] Map field-level errors for validation exceptions
  - [x] Fix typo in middleware filename (ExceptionHanldingMiddleware → ExceptionHandlingMiddleware)

## Phase 3: Testing & Validation (Effort: 1.5 hours)

- [x] Run full test suite
  - [x] Verify no regressions (134 tests passing)
  - [x] Existing exception handling tests still pass
  - [x] No breaking changes to dependent code

- [x] Manual testing of error responses
  - [x] Test validation error (422 with invalid data)
  - [x] Test not found error (404)
  - [x] Test business logic error (400)
  - [x] Test internal server error (simulate)
  - [x] Verify TraceId populated correctly
  - [x] Verify field-level errors formatted correctly

- [x] Verify OpenAPI schema includes ErrorResponse
  - [x] Sample endpoint metadata declares ErrorResponse response types
  - [x] Error codes documented

## Phase 4: Documentation & Cleanup (Effort: 1 hour)

- [x] Update API documentation with error response examples
  - [x] Add example 422 Unprocessable Entity validation response
  - [x] Add example 404 Not Found response
  - [x] Add example 409 Conflict response
  - [x] Add example 500 Internal Server Error response

- [x] Document error codes in architecture guide
  - [x] List all error codes and HTTP status codes
  - [x] Explain when each error code is used
  - [x] Provide example responses for each

## Success Criteria

- ✅ ErrorResponse DTO created with all required fields
- ✅ ErrorCodes constants defined and used
- ✅ ExceptionHandlingMiddleware returns standardized structure
- ✅ All validation errors include field-level details
- ✅ All 134+ tests passing (no regression)
- ✅ TraceId properly populated from HttpContext
- ✅ Error response structure documented in OpenAPI

## Testing Checklist

- [x] Validation error test (422 with errors dict)
- [x] Not found error test (404)
- [x] Conflict error test (409)
- [x] Unauthorized error test (403)
- [x] Business logic error test (400)
- [x] Internal server error test (500)
- [x] TraceId correlation test
- [x] Field-level error mapping test
- [x] All existing tests still pass

## Notes

- All changes isolated to API layer (middleware and DTOs)
- No domain layer changes required
- No breaking changes to existing code
- Error codes provide foundation for client-side error handling
- TraceId enables error tracking and correlation
- Future: Consider integrating with error monitoring service (Sentry, ApplicationInsights)

## Rollback Instructions

If issues discovered:
1. `git revert` the middleware changes
2. Remove `ErrorResponse.cs` and `ErrorCodes.cs`
3. Run test suite to verify
