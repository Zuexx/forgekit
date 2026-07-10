# API Error Responses Reference

## Overview

All API errors use a stable `ErrorResponse` shape. Clients can rely on `code` for programmatic handling, `errors` for field-level validation messages, and `traceId` for log correlation.

## Response Shape

```json
{
  "message": "One or more validation errors occurred",
  "code": "VALIDATION_ERROR",
  "timestamp": "2026-07-08T08:30:00Z",
  "traceId": "0HN1234567890",
  "errors": {
    "name": ["Name is required"]
  },
  "title": "Validation Error",
  "status": 422,
  "detail": "One or more validation errors occurred"
}
```

`errors` is optional and appears only when validation or domain rules provide field-level details.

## Error Codes

| Code | HTTP Status | Use |
| --- | ---: | --- |
| `VALIDATION_ERROR` | 422 | Request body, route, or command/query validation failed |
| `RESOURCE_NOT_FOUND` | 404 | Requested resource does not exist |
| `CONFLICT_ERROR` | 409 | Request conflicts with existing data or state |
| `UNAUTHORIZED_ERROR` | 403 | Authenticated user is not allowed to perform the operation |
| `BUSINESS_LOGIC_ERROR` | 400 | Legacy/general business rule failure |
| `INVALID_STATE_ERROR` | 400 | Resource state does not allow the requested operation |
| `INTERNAL_SERVER_ERROR` | 500 | Unexpected server failure |

## Examples

### 422 Validation Error

```json
{
  "message": "One or more validation errors occurred",
  "code": "VALIDATION_ERROR",
  "timestamp": "2026-07-08T08:30:00Z",
  "traceId": "trace-123",
  "errors": {
    "name": ["Name is required"],
    "dueDate": ["Due date must be in the future"]
  },
  "title": "Validation Error",
  "status": 422,
  "detail": "One or more validation errors occurred"
}
```

### 404 Not Found

```json
{
  "message": "Resource not found",
  "code": "RESOURCE_NOT_FOUND",
  "timestamp": "2026-07-08T08:30:00Z",
  "traceId": "trace-123",
  "title": "Not Found",
  "status": 404,
  "detail": "Resource not found"
}
```

### 409 Conflict

```json
{
  "message": "A resource with this name already exists",
  "code": "CONFLICT_ERROR",
  "timestamp": "2026-07-08T08:30:00Z",
  "traceId": "trace-123",
  "title": "Conflict",
  "status": 409,
  "detail": "A resource with this name already exists"
}
```

### 500 Internal Server Error

```json
{
  "message": "Unexpected error",
  "code": "INTERNAL_SERVER_ERROR",
  "timestamp": "2026-07-08T08:30:00Z",
  "traceId": "trace-123",
  "title": "Server Error",
  "status": 500,
  "detail": "Unexpected error"
}
```

## Client Handling

```typescript
type ErrorResponse = {
  message: string;
  code?: string;
  timestamp: string;
  traceId?: string;
  errors?: Record<string, string[]>;
  title?: string;
  status?: number;
  detail?: string;
};

function handleApiError(error: ErrorResponse) {
  switch (error.code) {
    case "VALIDATION_ERROR":
      return showFieldErrors(error.errors ?? {});
    case "RESOURCE_NOT_FOUND":
      return showNotFoundMessage();
    case "CONFLICT_ERROR":
      return showConflictMessage(error.message);
    case "UNAUTHORIZED_ERROR":
      return showForbiddenMessage();
    default:
      return showGenericError(error.traceId);
  }
}
```

## Trace IDs

Every error includes a `traceId`. When reporting production issues, include this value so server logs can be searched for the exact request.
