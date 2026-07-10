# Change: Add Structured Logging

## Why

The application currently has basic exception logging in middleware, but lacks **structured logging** throughout the application layer (handlers, services, domain logic). This makes it difficult to:
- Trace request flows across components
- Debug issues in production without detailed telemetry
- Correlate logs by request context (user, operation, tenant)
- Implement observability practices (monitoring, alerting, analytics)

Structured logging with correlation IDs enables better diagnostics and operational insights across the entire application lifecycle.

## What Changes

- Add Serilog as the structured logging framework
- Configure console and file-based logging with rolling intervals
- Add correlation ID middleware to track requests end-to-end
- Implement structured logging patterns in:
  - MediatR handlers (commands and queries)
  - Application services
  - Domain services
  - Critical business operations
- Log key business events and transitions with context
- Standardize log levels and metadata enrichment

## Impact

- **Affected specs:** New spec `observability/spec.md` (observability and logging)
- **Affected code:** 
  - Application bootstrap configuration (logging setup)
  - MediatR handlers or request processors
  - Service classes in application layer
  - New middleware for correlation ID tracking
  - New configuration options for logging

## Breaking Changes

None. This is purely additive—existing exception handling continues to work.

## Dependencies

- Serilog.AspNetCore NuGet package
- Serilog.Sinks.Console (included with AspNetCore)
- Serilog.Sinks.File (included with AspNetCore)
