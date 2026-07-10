## Design: Structured Logging with Serilog

### Context

The application needs comprehensive observability to support production debugging, auditing, and operational monitoring. Current basic exception logging in middleware is insufficient for:
- Tracing request flows across multiple handlers and services
- Debugging complex business logic failures
- Compliance auditing (who performed what action and when)
- Performance monitoring and bottleneck identification
- Distributed tracing correlation across services (future)

### Goals

- **Primary:** Implement structured logging framework for all application layers
- **Secondary:** Enable correlation ID tracking across request lifecycle
- **Secondary:** Standardize logging patterns for consistency
- **Non-Goal:** Replace exception handling—logging complements existing exception handling middleware
- **Non-Goal:** Implement distributed tracing (service-to-service)—correlation IDs prepare for it

### Decisions

#### Decision 1: Use Serilog as Logging Framework
**What:** Adopt Serilog for structured logging instead of built-in ILogger alone.

**Why:**
- Industry standard for .NET structured logging
- Excellent enrichment capabilities for adding context automatically
- Multiple sink options (console, file, cloud providers, etc.)
- Strong support for structured properties and semantic logging
- Mature ecosystem with broad community adoption

**Alternatives considered:**
- Built-in `ILogger<T>`: Would work but lacks enrichment and structured properties
- NLog: Also good, but Serilog is more idiomatic for structured logging
- Application Insights: Too heavyweight for initial implementation; can add later as additional sink

**Decision:** ✅ Serilog is the primary structured logging provider

---

#### Decision 2: Dual Sinks - Console and File
**What:** Configure both console and file sinks for logs.

**Why:**
- Console: Immediate visibility during development and container orchestration (Docker logs)
- File: Persistent audit trail and analysis for production issues
- Combined approach provides both real-time and historical visibility

**File rolling strategy:**
- Daily rolling interval balances log size and historical retention
- Allows grep/analysis of specific business events by date
- Production ops can set retention policies per business requirements

**Alternatives considered:**
- Console only: Would lose historical audit trail
- File only: Would make development harder and container logging invisible
- Cloud sink (e.g., Application Insights, Datadog): Add later as separate sink

**Decision:** ✅ Both console and file sinks; file rolls daily

---

#### Decision 3: Correlation ID via Middleware
**What:** Inject correlation ID into log context per HTTP request.

**Why:**
- Enables end-to-end request tracing across all layers
- Standard practice for debugging multi-layered applications
- Prepares architecture for distributed tracing (future)
- Minimal performance overhead

**Implementation approach:**
- Extract from request header if present (e.g., `X-Correlation-ID`)
- Generate new GUID if not provided (ensures every request has one)
- Inject into Serilog LogContext (available to all logs in that request scope)
- Return correlation ID in response headers for client debugging

**Alternatives considered:**
- No correlation ID: Would lose ability to correlate logs from single request
- Application-level tracking only: Would not be available to middleware/infrastructure logs

**Decision:** ✅ Middleware-based correlation ID with LogContext injection

---

#### Decision 4: Log Levels and Usage
**What:** Establish clear log level semantics.

**Semantics:**
- **Debug:** Detailed diagnostic information (parameter values, state transitions) - Development/Testing
- **Information:** Important business events (entity created, status changed, user action)
- **Warning:** Recoverable issues (retry attempt, deprecated API, fallback behavior)
- **Error:** Operation failed but application continues (validation failed, not found)
- **Fatal:** Application cannot continue (critical infrastructure failure)

**Minimum levels by environment:**
- Development: Debug
- Staging: Information
- Production: Information (can adjust to Warning if too noisy)

**Why this approach:**
- Clear semantics prevent "noise" from excessive logging
- Information level captures all business events (audit trail)
- Debug level kept out of production by default (performance)
- Operators can adjust minimum level per namespace if needed

**Decision:** ✅ Structured log levels with environment-based minimums

---

#### Decision 5: Enrichment Properties
**What:** Automatically inject structured properties into all logs.

**Properties added:**
- `Application` = Application name (identify logs from this system)
- `CorrelationId` = request correlation ID (trace request flow)
- `UserId` = authenticated user ID when available (audit who took action)
- `Environment` = Development/Staging/Production (context)
- `Timestamp` = ISO 8601 UTC (standardized time)

**Why:**
- Structured properties enable filtering and aggregation in log analysis tools
- Automatic enrichment prevents developers from forgetting to include context
- Supports future integration with centralized logging platforms

**Decision:** ✅ Automatic enrichment via Serilog middleware and LogContext

---

#### Decision 6: Handler and Service Logging Patterns
**What:** Standardize logging within request handlers and service layer execution.

**Pattern for command handlers:**
```csharp
public class CreateResourceHandler : IRequestHandler<CreateResourceCommand, CreateResourceResponse>
{
    public async Task<CreateResourceResponse> Handle(CreateResourceCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Creating resource with key {ResourceKey} for user {UserId}", 
            request.ResourceKey, request.UserId);
        
        try
        {
            var result = await _service.CreateResourceAsync(request.ResourceKey, request.UserId, ct);
            _logger.LogInformation("Resource created with ID {ResourceId}", result.Id);
            return new CreateResourceResponse { ResourceId = result.Id };
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Resource creation failed: {Reason}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating resource");
            throw;
        }
    }
}
```

**Pattern for service layer:**
```csharp
public class ResourceService
{
    public async Task<Resource> CreateResourceAsync(string resourceKey, string userId, CancellationToken ct)
    {
        _logger.LogDebug("Validating resource key {ResourceKey} authorization for user {UserId}", 
            resourceKey, userId);
        
        var resource = await _repository.GetByKeyAsync(resourceKey, ct);
        if (resource == null)
        {
            _logger.LogWarning("Resource not found: {ResourceKey}", resourceKey);
            throw new NotFoundException("Resource not found");
        }
        
        // ... more logic ...
        
        _logger.LogInformation("Resource operation completed for {ResourceKey}: {ResourceId}", 
            resourceKey, resource.Id);
        return resource;
    }
}
```

**Why this pattern:**
- Entry log with parameters helps trace execution flow
- Success log confirms business operation completed
- Error logs capture reason for failure
- Structured properties enable filtering by entity ID or user

**Decision:** ✅ Standardized handler/service logging with entry, success, and error logs

---

#### Decision 7: Configuration and Flexibility
**What:** Make logging behavior configurable via appsettings.json.

**Configurable aspects:**
- Minimum log level (global and per namespace)
- Log file path and retention
- Serilog output template customization
- Whether to log to console, file, or both

**Why:**
- Operators can adjust logging without code changes
- Different environments can have different levels
- Easy to disable noisy loggers (e.g., Microsoft.EntityFrameworkCore)
- Supports migration to cloud logging providers later

**Decision:** ✅ Serilog configuration via appsettings.json and Program.cs

---

### Risks and Mitigation

| Risk | Impact | Mitigation |
|------|--------|-----------|
| **Excessive logging impacts performance** | High | Use Debug level judiciously; set minimum to Information in Production; measure impact with APM tools |
| **Log files consume disk space** | Medium | Implement retention policy (keep 30 days); monitor disk usage; use log aggregation for long-term storage |
| **Sensitive data logged by mistake** | High | Code review for PII; never log passwords, tokens, credit cards; redact in middleware if needed |
| **Correlation ID not propagated in async operations** | Medium | Use Serilog LogContext which is AsyncLocal; test async scenarios thoroughly |
| **Noisy third-party library logs** | Low | Configure minimum level per namespace in appsettings.json (e.g., Microsoft.* = Warning) |

---

### Migration Plan

**Phase 1: Infrastructure (Week 1)**
1. Install Serilog packages
2. Configure Serilog in Program.cs
3. Add correlation ID middleware
4. Verify console and file output

**Phase 2: Handler Logging (Week 1-2)**
1. Add logging to existing MediatR handlers
2. Standardize log messages
3. Write handler logging tests

**Phase 3: Service Logging (Week 2)**
1. Add logging to domain services
2. Log business rule validation
3. Log state transitions

**Phase 4: Testing and Documentation (Week 2-3)**
1. Integration tests for end-to-end logging
2. Update project documentation
3. Create logging best practices guide

---

### Open Questions

- **Q:** Should we log at application layer or also at domain layer (entities)?
  - **A:** Log at application and service layers; keep domain entities focused on business logic

- **Q:** How to handle logging in background jobs or scheduled tasks?
  - **A:** Same correlation ID approach; generate request ID for background operations if needed

- **Q:** Should we implement custom Serilog enricher for specific properties?
  - **A:** Use LogContext for request-scoped properties; custom enricher only if needed for application-wide properties

- **Q:** How to prevent accidental logging of sensitive data?
  - **A:** Code review, unit tests, and use redaction filters if needed for structured properties
