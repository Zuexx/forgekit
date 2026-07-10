# ForgeKit API Glossary

A comprehensive glossary of terms, patterns, and concepts used in the ForgeKit API project.

---

## A

### Abstract Validator
A base class from **FluentValidation** used to define validation rules for DTOs and commands. Validators are automatically discovered and executed by the `ValidationBehavior` in the MediatR pipeline.

**Example:** `CreateResourceCommandValidator : AbstractValidator<CreateResourceCommand>`

**Related:** [FluentValidation](#fluentvalidation), [ValidationBehavior](#validationbehavior)

---

### ADR (Architecture Decision Record)
A document that captures an important architectural decision along with its context, rationale, alternatives considered, and consequences. ADRs are stored in `docs/adr/`.

**Example:** `ADR-001: Module/Feature Pattern`

**Related:** [Module Pattern](#module-pattern)

---

### API Error Code
A standardized error code returned in error responses to identify the type of error. Error codes are defined in `ForgeKit.Api/Constants/ErrorCodes.cs`.

**Common codes:**
- `VALIDATION_ERROR` - Input validation failed
- `RESOURCE_NOT_FOUND` - Requested resource doesn't exist
- `UNAUTHORIZED` - Authentication required
- `FORBIDDEN` - Insufficient permissions
- `CONFLICT` - Resource already exists

**Related:** [Error Response Standardization](#error-response-standardization), [RFC 7807](#rfc-7807)

---

### Application Service
A service layer that orchestrates business logic, coordinates between domain services, and implements application-specific workflows. Application services are registered via `RegisterApplicationServices()`.

**Location:** `ForgeKit.Api/Services/`

**Example:** `TodoService`

**Related:** [Domain Service](#domain-service), [Service Layer](#service-layer)

---

### Audit Context
The execution context containing information about the current user performing an operation, used for audit trail purposes. Extracted by `AuditContextService`.

**Fields populated:**
- `CreatedBy` / `UpdatedBy` - User ID from JWT token
- `CreatedAt` / `UpdatedAt` - Timestamps

**Related:** [Audit Trail](#audit-trail), [IAuditContextService](#iauditcontextservice)

---

### Audit Trail
A record of who created, modified, or deleted entities and when. Implemented via `BaseEntity` fields:
- `CreatedAt` (DateTime)
- `CreatedBy` (string)
- `UpdatedAt` (DateTime?)
- `UpdatedBy` (string?)
- `DeletedAt` (DateTime?) - For soft-deleted entities
- `DeletedBy` (string?)

**Related:** [Soft Delete](#soft-delete), [BaseEntity](#baseentity)

---

## B

### Base Entity
An abstract base class (`ForgeKit.Api/Entities/Base/BaseEntity.cs`) that all domain entities inherit from. Provides common fields for audit trail and soft delete.

**Fields:**
- `Id` (string) - Primary key
- `CreatedAt`, `CreatedBy` - Audit fields
- `UpdatedAt`, `UpdatedBy` - Audit fields
- `IsDeleted`, `DeletedAt`, `DeletedBy` - Soft delete fields

**Related:** [Audit Trail](#audit-trail), [Soft Delete](#soft-delete)

---

### Better Auth
An authentication database context (`BetterAuthDbContext`) used for managing authentication-related entities separately from business data.

**Location:** `ForgeKit.Api/Data/Auth/BetterAuthDbContext.cs`

**Related:** [JWT](#jwt-json-web-token), [Authentication](#authentication)

---

## C

### Command
A request to **change** system state (Create, Update, Delete). Commands implement `IRequest<Result<T>>` and are handled by a `CommandHandler`.

**Example:** `CreateResourceCommand`, `UpdateTodoStatusCommand`, `DeleteResourceCommand`

**Related:** [CQRS](#cqrs-command-query-responsibility-segregation), [Query](#query), [MediatR](#mediatr)

---

### Command Handler
A class that processes a `Command` and returns a `Result<T>`. Inherits from `ResultCommandHandler<TCommand, TResponse>`.

**Example:** `CreateResourceCommandHandler`

**Related:** [Command](#command), [Result Pattern](#result-pattern)

---

### Correlation ID
A unique identifier (`X-Correlation-ID` header) that tracks a request through the entire system, including all log entries. Useful for distributed tracing and debugging.

**Format:** GUID (e.g., `3fa85f64-5717-4562-b3fc-2c963f66afa6`)

**Related:** [CorrelationIdMiddleware](#correlationidmiddleware), [Structured Logging](#structured-logging)

---

### CQRS (Command Query Responsibility Segregation)
An architectural pattern that separates **read operations** (Queries) from **write operations** (Commands). Implemented via MediatR.

**Benefits:**
- ✅ Clearer separation of concerns
- ✅ Independent scaling of reads and writes
- ✅ Optimized queries without complex domain logic

**Related:** [Command](#command), [Query](#query), [MediatR](#mediatr)

---

## D

### DbContext
Entity Framework Core's main class for database operations. The project uses:
- `AppDbContext` - Business data
- `BetterAuthDbContext` - Authentication data

**Related:** [Entity Framework Core](#entity-framework-core-ef-core), [Unit of Work](#unit-of-work)

---

### Domain Service
A service that encapsulates **domain logic** that doesn't naturally fit within a single entity. Domain services are stateless and registered via `RegisterDomainServices()`.

**Location:** `ForgeKit.Api/Domain/Services/`

**Example:** `SoftDeleteDomainService`

**Related:** [Application Service](#application-service), [Service Layer](#service-layer)

---

### DTO (Data Transfer Object)
An object used to transfer data between layers or across network boundaries. DTOs are typically immutable `record` types.

**Example:** `ResourceDto`, `TodoItemDto`

**Related:** [Record Type](#record-type), [Mapping](#mapping)

---

## E

### Entity Framework Core (EF Core)
An Object-Relational Mapper (ORM) used for database access. The project uses **EF Core 10** with PostgreSQL.

**Key features:**
- ✅ LINQ queries
- ✅ Change tracking
- ✅ Migrations
- ✅ Query filters (for soft delete)

**Related:** [DbContext](#dbcontext), [Migration](#migration)

---

### Error Response Standardization
A pattern for returning consistent error responses across all endpoints, following **RFC 7807** (Problem Details).

**Standard format:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred",
  "errors": { "field": ["error message"] },
  "traceId": "correlation-id"
}
```

**Related:** [RFC 7807](#rfc-7807), [API Error Code](#api-error-code)

---

### Exception Handling Middleware
Middleware (`ExceptionHandlingMiddleware`) that catches unhandled exceptions, logs them with correlation IDs, and returns standardized error responses.

**Location:** `ForgeKit.Api/Middlewares/ExceptionHandlingMiddleware.cs`

**Related:** [Middleware](#middleware), [Correlation ID](#correlation-id)

---

## F

### FluentValidation
A .NET library for building strongly-typed validation rules. Validators are automatically discovered and executed via `ValidationBehavior`.

**Example:**
```csharp
RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
```

**Related:** [Abstract Validator](#abstract-validator), [ValidationBehavior](#validationbehavior)

---

## G

### Grace Period
The time window (default: **30 days**) during which a soft-deleted entity can be restored. After the grace period, entities are considered permanently deleted.

**Configuration:** Currently hardcoded in `SoftDeleteDomainService.CanRestore()` (default parameter: 30 days)

**Future:** Should be moved to `appsettings.json` for configurability.

**Related:** [Soft Delete](#soft-delete), [Restore](#restore)

---

### Global Query Filter
An EF Core feature that automatically applies a filter to all queries for a given entity type. Used to exclude soft-deleted records.

**Example:**
```csharp
modelBuilder.Entity<MyEntity>()
    .HasQueryFilter(e => !e.IsDeleted);
```

**Related:** [Soft Delete](#soft-delete), [Entity Framework Core](#entity-framework-core-ef-core)

---

## H

### Handler
A generic term for classes that process requests in the MediatR pipeline. Includes:
- **Command Handlers** - Process commands (writes)
- **Query Handlers** - Process queries (reads)

**Related:** [Command Handler](#command-handler), [Query Handler](#query-handler)

---

## I

### IAuditContextService
An interface for retrieving the current user's identity for audit trail purposes.

**Methods:**
- `GetCurrentUserId()` - Returns user ID from JWT claims or "system"

**Implementation:** `AuditContextService`

**Related:** [Audit Context](#audit-context), [Audit Trail](#audit-trail)

---

### IModule
An interface that defines a feature module. Modules register services and map endpoints.

**Methods:**
- `RegisterModule(IServiceCollection)` - Register dependencies
- `MapEndpoints(IEndpointRouteBuilder)` - Define HTTP endpoints

**Related:** [Module Pattern](#module-pattern), [ISampleModule](#isamplemodule)

---

### ISampleModule
A marker interface that extends `IModule` to identify sample/demo modules. Sample modules are automatically **excluded from production** environments.

**Purpose:**
- ✅ Keep demo/testing endpoints out of production
- ✅ Automatically filtered by environment check

**Related:** [IModule](#imodule), [Sample Code](#sample-code)

---

### IUnitOfWork
An interface that manages database transactions and provides access to repositories/DbSets.

**Methods:**
- `SaveChangesAsync()` - Persist changes with audit trail
- `BeginTransaction()` - Start explicit transaction
- `CommitTransaction()` - Commit transaction
- `RollbackTransaction()` - Rollback transaction

**Implementation:** `UnitOfWork`

**Related:** [Unit of Work](#unit-of-work), [Transaction](#transaction)

---

## J

### JWT (JSON Web Token)
A compact, URL-safe token format used for authentication. The API validates JWTs using public keys from a **JWKS endpoint**.

**Structure:**
- **Header** - Algorithm and token type
- **Payload** - Claims (user ID, roles, expiration)
- **Signature** - Cryptographic signature

**Related:** [JWKS](#jwks-json-web-key-set), [Authentication](#authentication)

---

### JWKS (JSON Web Key Set)
A set of public keys used to verify JWT signatures. The API fetches JWKS from the authentication server.

**Endpoint example:** `https://auth.yourdomain.com/.well-known/jwks.json`

**Related:** [JWT](#jwt-json-web-token), [JwksProvider](#jwksprovider)

---

### JwksProvider
A singleton service (`IJwksProvider`) that lazily loads and caches JWKS from the authentication server.

**Location:** `ForgeKit.Api/Data/Auth/JwksProvider.cs`

**Related:** [JWKS](#jwks-json-web-key-set)

---

## M

### Mapping
The process of converting between entities and DTOs. Currently done manually in handlers.

**Future consideration:** AutoMapper or Mapster for automatic mapping.

**Related:** [DTO](#dto-data-transfer-object)

---

### MediatR
A library that implements the **Mediator pattern** for in-process messaging. Used to implement CQRS.

**Benefits:**
- ✅ Decouples controllers from handlers
- ✅ Supports pipeline behaviors (validation, logging)
- ✅ Simplifies testing

**Related:** [CQRS](#cqrs-command-query-responsibility-segregation), [Pipeline Behavior](#pipeline-behavior)

---

### Middleware
A component in the ASP.NET Core request pipeline that processes HTTP requests/responses.

**Custom middleware in project:**
- `CorrelationIdMiddleware` - Adds correlation IDs
- `ExceptionHandlingMiddleware` - Handles exceptions

**Related:** [Pipeline](#pipeline)

---

### Migration
A version-controlled database schema change managed by Entity Framework Core.

**Commands:**
```bash
dotnet ef migrations add DescriptionOfChange --project ForgeKit.Api.Migrations.Sqlite --startup-project ForgeKit.Api.Migrations.Sqlite --context AppDbContext --output-dir Migrations/App
dotnet ef database update --project ForgeKit.Api.Migrations.Sqlite --startup-project ForgeKit.Api.Migrations.Sqlite --context AppDbContext
```

**Locations:** `ForgeKit.Api.Migrations.Sqlite/Migrations/`, `ForgeKit.Api.Migrations.Postgres/Migrations/`, and `ForgeKit.Api.Migrations.SqlServer/Migrations/`

**Related:** [Entity Framework Core](#entity-framework-core-ef-core)

---

### Module Pattern
An architectural pattern (ADR-001) for organizing features into self-contained modules. Each module registers its own dependencies and endpoints.

**Benefits:**
- ✅ Clear feature boundaries
- ✅ Easy to add/remove features
- ✅ Testable in isolation

**Related:** [IModule](#imodule), [ADR](#adr-architecture-decision-record)

---

## O

### OpenAPI
A specification for describing REST APIs. The project auto-generates OpenAPI documentation (Swagger).

**Endpoint:** `/scalar/v1`

**Related:** [Scalar](#scalar), [Swagger](#swagger)

---

## P

### Pipeline Behavior
A MediatR feature that intercepts requests before they reach handlers. Used for cross-cutting concerns.

**Example:** `ValidationBehavior<TRequest, TResponse>` validates all requests automatically.

**Related:** [MediatR](#mediatr), [ValidationBehavior](#validationbehavior)

---

### Pipeline (ASP.NET Core)
The sequence of middleware components that process HTTP requests and responses.

**Order matters:**
1. CorrelationIdMiddleware
2. ExceptionHandlingMiddleware
3. Authentication
4. Authorization
5. Endpoint routing

**Related:** [Middleware](#middleware)

---

## Q

### Query
A request to **read** system state without modification. Queries implement `IRequest<Result<T>>` and are handled by a `QueryHandler`.

**Example:** `GetResourceByIdQuery`, `ListResourcesQuery`

**Related:** [CQRS](#cqrs-command-query-responsibility-segregation), [Command](#command)

---

### Query Filter
See [Global Query Filter](#global-query-filter)

---

### Query Handler
A class that processes a `Query` and returns a `Result<T>`. Inherits from `ResultQueryHandler<TQuery, TResponse>`.

**Example:** `GetResourceByIdQueryHandler`

**Related:** [Query](#query), [Result Pattern](#result-pattern)

---

## R

### Railway-Oriented Programming (ROP)
A functional programming pattern for error handling where operations return `Success` or `Failure` instead of throwing exceptions.

**Visualization:**
```
Success Track:  ──────────────────────────►
                     ▼ (error occurs)
Failure Track:  ───────────────────────────►
```

**Related:** [Result Pattern](#result-pattern)

---

### Record Type
A C# reference type designed for immutable data. Used for DTOs, commands, and queries.

**Example:**
```csharp
public record ResourceDto(string Id, string Name);
```

**Related:** [DTO](#dto-data-transfer-object)

---

### Repository Pattern
**Note:** This project **does NOT use the Repository pattern** (see ADR-004). Instead, handlers directly use `DbContext` or `IUnitOfWork`.

**Rationale:** EF Core's `DbSet<T>` already provides repository-like functionality.

**Related:** [DbContext](#dbcontext), [Unit of Work](#unit-of-work)

---

### Result Pattern
A type-safe error handling pattern using discriminated unions. Operations return `Result<T>.Success` or `Result<T>.Failure`.

**Benefits:**
- ✅ Explicit error handling
- ✅ No exception throwing overhead
- ✅ Compiler-enforced handling

**Example:**
```csharp
return item != null 
    ? Success(item) 
    : Failure("NOT_FOUND", "Item not found");
```

**Related:** [Railway-Oriented Programming](#railway-oriented-programming-rop), [Success](#success), [Failure](#failure)

---

### Restore
The process of undeleting a soft-deleted entity, setting `IsDeleted = false` and clearing `DeletedAt`/`DeletedBy`.

**Restrictions:**
- Can only restore within grace period (default 30 days)
- Handled by `SoftDeleteDomainService.Restore()`

**Related:** [Soft Delete](#soft-delete), [Grace Period](#grace-period)

---

### RFC 7807
An IETF standard for HTTP API error responses (Problem Details for HTTP APIs). Defines a consistent JSON format for errors.

**Related:** [Error Response Standardization](#error-response-standardization)

---

## S

### Sample Code
Code located in `ForgeKit.Api/Samples/` that demonstrates patterns and serves as templates for developers. Sample modules implement `ISampleModule` and are excluded from production.

**Purpose:**
- ✅ Reference implementation
- ✅ Testing patterns
- ❌ Not for production use

**Related:** [ISampleModule](#isamplemodule)

---

### Scalar
An interactive API documentation UI (alternative to Swagger UI). Provides a modern interface for exploring and testing endpoints.

**Endpoint:** `/scalar/v1`

**Related:** [OpenAPI](#openapi), [Swagger](#swagger)

---

### Scoped Lifetime
A dependency injection lifetime where one instance is created per HTTP request.

**Used for:**
- `AppDbContext`
- `IUnitOfWork`
- `IAuditContextService`

**Related:** [Dependency Injection](#dependency-injection)

---

### Serilog
A structured logging library used for application logging. Configured in `appsettings.json`.

**Features:**
- ✅ Structured logs (JSON-friendly)
- ✅ Multiple sinks (Console, File, Seq, Application Insights)
- ✅ Contextual enrichment (correlation IDs)

**Related:** [Structured Logging](#structured-logging)

---

### Service Layer
An application architecture layer containing business logic. Divided into:
- **Domain Services** - Pure business logic
- **Application Services** - Orchestration and workflows

**Related:** [Domain Service](#domain-service), [Application Service](#application-service)

---

### Singleton Lifetime
A dependency injection lifetime where one instance is created for the application lifetime.

**Used for:**
- `IJwksProvider`

**Related:** [Dependency Injection](#dependency-injection)

---

### Soft Delete
A pattern where entities are marked as deleted (`IsDeleted = true`) instead of being physically removed from the database.

**Benefits:**
- ✅ Data recovery possible
- ✅ Audit trail preserved
- ✅ Referential integrity maintained

**Implementation:**
- `BaseEntity.IsDeleted`, `DeletedAt`, `DeletedBy`
- Global query filter excludes deleted entities
- `SoftDeleteDomainService` handles marking/restoring

**Related:** [Grace Period](#grace-period), [Restore](#restore), [BaseEntity](#baseentity)

---

### Structured Logging
A logging approach where log entries are structured data (key-value pairs) instead of plain text strings.

**Example:**
```csharp
Logger.LogInformation("User {UserId} created resource {ResourceId}", userId, resourceId);
// Output: { "UserId": "user123", "ResourceId": "res456", "Message": "User created resource" }
```

**Benefits:**
- ✅ Easy to query and filter
- ✅ Machine-readable
- ✅ Better for log aggregation tools

**Related:** [Serilog](#serilog), [Correlation ID](#correlation-id)

---

### Success
A case of `Result<T>` representing a successful operation with data.

**Usage:**
```csharp
return Success(myData);
// Returns: Result<T>.Success(myData)
```

**Related:** [Result Pattern](#result-pattern), [Failure](#failure)

---

### Swagger
A tool for generating interactive API documentation from OpenAPI specifications.

**Note:** This project uses **Scalar** instead of the default Swagger UI for a better user experience.

**Related:** [OpenAPI](#openapi), [Scalar](#scalar)

---

## T

### Transaction
A database operation that ensures atomicity (all-or-nothing). Managed by `IUnitOfWork`.

**Methods:**
- `BeginTransaction()` - Start
- `CommitTransaction()` - Save
- `RollbackTransaction()` - Undo

**Related:** [Unit of Work](#unit-of-work), [IUnitOfWork](#iunitofwork)

---

### Trace ID
See [Correlation ID](#correlation-id). Used interchangeably in error responses.

---

## U

### Unit of Work
A pattern that maintains a list of objects affected by a business transaction and coordinates writing changes. Implements `IUnitOfWork`.

**Responsibilities:**
- ✅ Coordinate multiple repository operations
- ✅ Manage transactions
- ✅ Ensure consistency
- ✅ Populate audit fields automatically

**Location:** `ForgeKit.Api/Data/UnitOfWork.cs`

**Related:** [IUnitOfWork](#iunitofwork), [Transaction](#transaction)

---

## V

### ValidationBehavior
A MediatR pipeline behavior that automatically validates all requests using FluentValidation validators.

**Process:**
1. Request enters pipeline
2. ValidationBehavior discovers validators for request type
3. Validators execute
4. If validation fails → `ValidationAppException` thrown
5. If validation passes → Handler executes

**Location:** `ForgeKit.Api/Behaviors/ValidationBehavior.cs`

**Related:** [Pipeline Behavior](#pipeline-behavior), [FluentValidation](#fluentvalidation)

---

### Validator
See [Abstract Validator](#abstract-validator)

---

## Acronyms

| Acronym | Full Name | Description |
|---------|-----------|-------------|
| **ADR** | Architecture Decision Record | Document capturing architectural decisions |
| **API** | Application Programming Interface | HTTP REST API |
| **CORS** | Cross-Origin Resource Sharing | Security feature for cross-domain requests |
| **CQRS** | Command Query Responsibility Segregation | Pattern separating reads and writes |
| **DTO** | Data Transfer Object | Object for transferring data between layers |
| **EF Core** | Entity Framework Core | ORM for database access |
| **GUID** | Globally Unique Identifier | 128-bit unique identifier |
| **HTTP** | HyperText Transfer Protocol | Application protocol |
| **JWT** | JSON Web Token | Token format for authentication |
| **JWKS** | JSON Web Key Set | Public keys for JWT validation |
| **ORM** | Object-Relational Mapper | Maps objects to database tables |
| **REST** | Representational State Transfer | Architectural style for APIs |
| **RFC** | Request for Comments | Internet standards document |
| **ROP** | Railway-Oriented Programming | Functional error handling pattern |
| **SQL** | Structured Query Language | Database query language |
| **TLS** | Transport Layer Security | Cryptographic protocol |
| **UI** | User Interface | Visual interface |
| **URI** | Uniform Resource Identifier | Resource identifier |

---

## See Also

- [USER_GUIDE.md](./USER_GUIDE.md) - Getting started guide
- [CONFIGURATION_GUIDE.md](./CONFIGURATION_GUIDE.md) - Configuration reference
- [RESULT_PATTERN_GUIDE.md](./RESULT_PATTERN_GUIDE.md) - Result pattern deep dive
- [FLUENT_VALIDATION_GUIDE.md](./FLUENT_VALIDATION_GUIDE.md) - Validation guide
- [EXCEPTION_HANDLING_GUIDE.md](./EXCEPTION_HANDLING_GUIDE.md) - Exception handling
- [docs/adr/](./adr/) - Architecture Decision Records

---

**Version:** 1.0  
**Last Updated:** February 2026  
**Maintained by:** Development Team
