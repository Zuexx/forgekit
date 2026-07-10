# Project Context

## Purpose
ForgeKit is a full-stack starter kit built with .NET 10, ASP.NET Core, and Next.js. It demonstrates TODO management as a reference domain implementation and exposes HTTP endpoints, persistence, authentication, and domain logic for managing Workspaces, Members, TodoItems, Categories, Labels, and analytics.

## Tech Stack
- .NET 10 (net10.0) running ASP.NET Core Web API
- Entity Framework Core with SQLite, PostgreSQL, and SQL Server providers
- MediatR for in-process request/command routing
- FluentValidation for input validation
- JWT Bearer authentication via Microsoft.AspNetCore.Authentication.JwtBearer
- OpenAPI/Swagger via Microsoft.AspNetCore.OpenApi
- SQLite as the default relational database; PostgreSQL and SQL Server are optional deployment providers

## Project Conventions

### Code Style
- Follow C# idioms and the default .NET formatting conventions (use dotnet-format in CI if configured).
- Prefer immutable DTOs where practical and nullable reference types are enabled (Nullable enabled in project).
- Use PascalCase for public types and members, camelCase for local variables and parameters.
- Keep controllers thin: push business logic into MediatR handlers and services.

### Logging Strategy
All logging is implemented using **Serilog** with structured logging and correlation ID tracking:

#### Framework & Setup
- **Logging Framework**: Serilog.AspNetCore
- **Output Sinks**: 
  - Console (real-time development visibility)
  - Rolling file logs (daily rolling in ForgeKit.Api/logs directory)
- **Structured Enrichment**: LogContext for correlation IDs, properties for domain-specific data
- **Configuration**: Serilog config in appsettings.json; per-namespace log level overrides available

#### Log Levels and Usage
| Level | When to Use | Examples |
|-------|------------|----------|
| **Debug** | Low-level execution details for troubleshooting | Handler entry/exit, query results, validation checks |
| **Information** | Business-significant events | Successful command execution, user actions, state changes |
| **Warning** | Recoverable issues or business rule violations | Request validation failure, conflict detected, retry attempt |
| **Error** | Exceptions and failures | Unhandled exceptions, critical operation failure |

#### Correlation ID Tracking
- **Generation**: Automatically generated per request via `CorrelationIdMiddleware`
- **Extraction**: Read from `X-Correlation-ID` request header if present; otherwise generated as GUID (no hyphens)
- **Propagation**: Injected into Serilog LogContext (AsyncLocal) for all downstream logs in the request
- **Response**: Returned in `X-Correlation-ID` response header for client tracing
- **Use Case**: Enables end-to-end request tracing across logs, middleware, handlers, and services

#### Handler Logging Patterns
**Command Handlers** (ResultCommandHandler base class):
```csharp
Log entry:    Debug level with handler and request names
Log success:  Information level with operation result
Log failure:  Warning level with error code and message
```

**Query Handlers** (ResultQueryHandler base class):
```csharp
Log entry:    Debug level with handler and request names
Log success:  Debug level (informational only, no business impact)
Log failure:  Debug level (query failures are typically not critical)
```

#### Service Logging Patterns
- **Validation Checks**: Debug level (developer interest)
- **Business Rules**: Warning level when rules prevent action
- **State Changes**: Information level with old/new state and audit context
- **Operations**: Include structured properties: IDs, status, user context

Example:
```csharp
logger.LogInformation(
    "Visit request created: {VisitId} by {UserId} from status {OldStatus} to {NewStatus}",
    visitId, userId, oldStatus, newStatus);
```

#### Sensitive Data Protection
- **Never log**: Passwords, API keys, tokens, SSNs, credit card numbers
- **Safe to log**: User IDs, request/response summaries, timestamps, operation counts
- **Context**: User names (from claims) are logged; tokens are never logged
- **Audit Trail**: All create/update operations log user context from `IAuditContext.UserId`

#### Development vs Production
**Development** (appsettings.Development.json):
- Minimum level: Debug
- Console output enabled (real-time visibility)
- File logging enabled (logs rolled daily in ForgeKit.Api/logs/)
- Per-namespace overrides suppress verbose framework logs (Microsoft.* = Warning)

**Production** (appsettings.json):
- Minimum level: Information
- Console output disabled (use log aggregation service instead)
- File logging enabled with strict retention
- Only critical business events and errors logged

#### Adding Logging to New Handlers/Services
1. Inject `ILogger<T>` in constructor:
   ```csharp
   public class MyHandler : ResultCommandHandler<MyCommand, MyDto>
   {
       private readonly ILogger<MyHandler> _logger;
       
       public MyHandler(ILogger<MyHandler> logger) : base(logger) { }
   }
   ```
2. Use base class logging methods (entry/success/failure automatically handled)
3. Add domain-specific logs in service methods:
   ```csharp
   logger.LogInformation("Operation completed: {OperationId} for {UserId}", opId, userId);
   ```

#### Testing Logging
- Unit tests inject mock loggers via NSubstitute
- Integration tests use WebApplicationFactory to capture real logs
- Handler/service logging is verified by checking mock logger was called with appropriate level

### Architecture Patterns
- Layered Web API: Controllers -> MediatR requests/handlers -> Services/Domain -> EF Core repositories/DbContext.
- Use MediatR for commands/queries and to keep controllers as composition roots only.
- Keep DbContexts and persistence behavior in `ForgeKit.Api`; keep provider-specific migration histories in dedicated migration projects.
- Use dependency injection (built-in ASP.NET Core DI) for all services.

### Testing Strategy
- Prefer unit tests for MediatR handlers, validators, and domain logic.
- Use SQLite for relational integration tests; reserve EF Core InMemory for narrow unit tests that do not depend on relational behavior.
- Validate request/response shapes with contract tests where possible and keep end-to-end tests outside this repo if they involve multiple services.

### Git Workflow
- Use feature branches off main (kebab-case, prefixed with feature/ or fix/ for bug fixes).
- Commit messages: short summary line followed by optional body; reference issue/PR when applicable.
- Create pull requests for review; include changelog notes for breaking changes.

## Domain Context
- Primary domain is TODO management: Workspaces group Members and TodoItems; Categories and Labels classify todos; TodoStatusHistory tracks state changes; WorkspaceAnalytics and DailyActivitySnapshot provide reporting and contribution-graph data.
- Users authenticate with JWT; authorization is role- or claim-based within handlers or via attributes on controllers.

## Important Constraints
- Database provider is configurable. SQLite is the starter-kit default; PostgreSQL and SQL Server are supported provider options.
- EF Core migrations are provider-specific. Do not reuse SQLite migrations for PostgreSQL or SQL Server, or vice versa.
- Authentication uses JWTs; tokens are validated with standard JWT middleware.
- Keep API backward-compatible where practical; breaking changes require a spec change proposal per openspec.

## External Dependencies
- SQLite requires no external service; PostgreSQL or SQL Server is required only when selected for API persistence.
- Any external identity provider or secret store should be documented here if used (none documented in repo by default).
- OpenAPI/Swagger is enabled for local API exploration and docs.
