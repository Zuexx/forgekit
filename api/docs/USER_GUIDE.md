# API User Guide

A comprehensive guide to building, using, and extending this modern .NET 10 Web API.

## Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [Architecture](#architecture)
4. [Core Concepts](#core-concepts)
5. [API Usage](#api-usage)
6. [Data Model](#data-model)
7. [Development Guide](#development-guide)
8. [Testing Guide](#testing-guide)
9. [Troubleshooting](#troubleshooting)
10. [Resources](#resources)

---

## Overview

This project is a modern **REST API** built with:

- **Framework**: .NET 10 with ASP.NET Core 10
- **Data Access**: Entity Framework Core 10
- **Database**: PostgreSQL
- **Architecture**: Clean Architecture with CQRS pattern
- **Error Handling**: Result Pattern (Railway-Oriented Programming)
- **Validation**: FluentValidation
- **Logging**: Serilog (structured logging)
- **Authentication**: JWT Bearer tokens
- **API Documentation**: OpenAPI/Swagger with Scalar UI

### Key Features

✅ **Modular Design** - Feature-based organization with IModule pattern  
✅ **Type Safety** - Discriminated unions for error handling  
✅ **CQRS Pattern** - Separation of reads (queries) and writes (commands)  
✅ **Automatic Validation** - FluentValidation integrated via MediatR pipeline  
✅ **Structured Logging** - Serilog with correlation IDs for request tracing  
✅ **Soft Delete** - Entities are marked deleted, not removed  
✅ **Audit Trail** - CreatedAt/UpdatedAt/CreatedBy/UpdatedBy on all entities  
✅ **OpenAPI** - Auto-generated API documentation  

---

## Quick Start

### Prerequisites

- .NET 10 SDK
- SQLite for the default local setup. PostgreSQL or SQL Server are optional production providers.
- Visual Studio 2022 or VS Code

### Setup

1. **Clone and navigate**
   ```bash
   git clone <repository-url>
   cd ForgeKit.sln
   ```

2. **Configure database connection**
   
   The default local provider is SQLite and works without external services. To use PostgreSQL or SQL Server, switch `Database:Provider` and generate migrations for that provider.
   ```json
   {
     "Database": {
       "Provider": "Sqlite"
     },
     "ConnectionStrings": {
       "Sqlite": "Data Source=./data/forgekit.db"
     }
   }
   ```

3. **Configure authentication**
   
   Update JWT settings:
   ```json
   {
     "JwtData": {
       "Issuer": "http://localhost:3000",
       "Audience": "http://localhost:3000"
     },
     "JwksCallBackUrl": {
       "Base": "http://localhost:3000",
       "Jwks": "http://localhost:3000/api/auth/jwks"
     }
   }
   ```

4. **Apply database migrations**
   ```bash
   cd api
   dotnet ef database update --project ForgeKit.Api.Migrations.Sqlite --startup-project ForgeKit.Api.Migrations.Sqlite --context AppDbContext
   dotnet ef database update --project ForgeKit.Api.Migrations.Sqlite --startup-project ForgeKit.Api.Migrations.Sqlite --context BetterAuthDbContext
   ```

5. **Start the API**
   ```bash
   dotnet run
   ```
   
   API runs on `https://localhost:7288`

### Access API Documentation

- **Scalar UI** (Interactive): `https://localhost:7288/scalar/v1`
- **OpenAPI JSON**: `https://localhost:7288/openapi/v1.json`

---

## Architecture

### Project Structure

```
project/
├── ForgeKit.Api/                          # Main API project
│   ├── Program.cs                # Application startup
│   ├── appsettings.json          # Default configuration
│   ├── appsettings.Development.json  # Dev configuration
│   │
│   ├── Entities/                 # EF Core data models
│   │   ├── BaseEntity.cs         # Base class with audit fields
│   │   ├── Base/                 # Base entity definitions
│   │   ├── Configuration/        # Configuration entities
│   │   ├── Core/                 # Core domain entities
│   │   ├── Educational/          # Educational content entities
│   │   ├── Visits/               # Visit/Request tracking
│   │   ├── Compliance/           # Audit and compliance
│   │   └── Analytics/            # Reporting entities
│   │
│   ├── Data/                     # Data access layer
│   │   ├── AppDbContext.cs      # EF Core DbContext
│   │   ├── UnitOfWork.cs         # Unit of Work pattern
│   │   └── Auth/                 # Authentication contexts
│   │
│   ├── Models/                   # DTOs and request models
│   │   └── *Dto.cs              # Data transfer objects
│   │
│   ├── Results/                  # Result pattern implementation
│   │   ├── Result.cs             # Result<T> definition
│   │   └── ResultExtensions.cs   # Extension methods
│   │
│   ├── Handlers/                 # MediatR request handlers
│   │   ├── ResultQueryHandler.cs  # Query handler base class
│   │   ├── ResultCommandHandler.cs # Command handler base class
│   │   └── [Domain]/             # Domain-specific handlers
│   │
│   ├── Modules/                  # API endpoint definitions
│   │   ├── IModule.cs            # Module interface
│   │   └── *Module.cs            # Feature modules
│   │
│   ├── Services/                 # Business logic services
│   │   └── [Domain]/             # Domain services
│   │
│   ├── Middlewares/              # ASP.NET Core middleware
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   └── CorrelationIdMiddleware.cs
│   │
│   ├── Extensions/               # Helper extensions
│   │   ├── ModuleExtension.cs
│   │   └── ResultEndpointExtensions.cs
│   │
│   ├── Behaviors/                # MediatR pipeline behaviors
│   │   └── ValidationBehavior.cs
│   │
│   ├── Foundations/              # Infrastructure
│   │   └── ConfigureJwtBearerOptions.cs
│   │
│   ├── Interfaces/               # Interface definitions
│   │   └── IUnitOfWork.cs
│   │
│   └── Constants/                # Application constants
│       └── AppSettingKeys.cs
│
├── ForgeKit.Api.Tests/                    # Test project
│   ├── Unit/                     # Unit tests
│   ├── Integration/              # Integration tests
│   └── Samples/                  # Test data factories
│
└── docs/                         # Documentation
    ├── USER_GUIDE.md             # This file
    ├── RESULT_PATTERN_GUIDE.md   # Result pattern details
    ├── EXCEPTION_HANDLING_GUIDE.md
    ├── COMMIT_CONVENTION.md
    └── logging.md
```

### Architectural Patterns

#### 1. **Modular Architecture via IModule**

```csharp
public interface IModule
{
    IServiceCollection RegisterModule(IServiceCollection services);
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}
```

Features are organized as modules that:
- Register dependencies in the DI container
- Define and map HTTP endpoints
- Are automatically discovered and loaded

#### 2. **CQRS via MediatR**

```
┌─────────────────────────────────────┐
│         HTTP Request                 │
└──────────────┬──────────────────────┘
               │
               ▼
        ┌──────────────┐
        │   MediatR    │
        └──────┬───────┘
               │
        ┌──────▼────────────┐
        │ Pipeline Behaviors│ (Validation, Logging, etc.)
        └──────┬────────────┘
               │
      ┌────────▼─────────┐
      │  Query/Command   │
      │    Handler       │
      └────────┬─────────┘
               │
        ┌──────▼──────────┐
        │  Result<T>      │
        │  - Success      │
        │  - Failure      │
        └──────┬──────────┘
               │
               ▼
        ┌──────────────────┐
        │  HTTP Response   │
        └──────────────────┘
```

#### 3. **Result Pattern (Railway-Oriented Programming)**

Instead of throwing exceptions for expected business errors, use typed `Result<T>`:

```csharp
public abstract record Result<T>
{
    public sealed record Success(T Data) : Result<T>;
    
    public sealed record Failure(
        string Code,
        string Message,
        IReadOnlyDictionary<string, string[]>? Details = null
    ) : Result<T>;
}
```

**Benefits:**
- ✅ Type-safe - Compiler ensures all cases handled
- ✅ Performant - No exception stack trace overhead
- ✅ Explicit - Error handling visible in code
- ✅ Composable - Supports Map, Bind operations

#### 4. **Unit of Work Pattern**

```csharp
public interface IUnitOfWork
{
    IRepository<Entity> Entities { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

Manages database transactions and repositories.

---

## Core Concepts

### Result Pattern

**Success case:**
```csharp
return Success(data);  // Result<T>.Success with data
```

**Failure case:**
```csharp
return Failure("ERROR_CODE", "User-friendly message");
return Failure("ERROR_CODE", "Message", fieldErrors);  // With details
```

**Usage in handler:**
```csharp
var result = await mediator.Send(query);

var response = result switch
{
    Result<DataDto>.Success success => 
        Results.Ok(success.Data),
    
    Result<DataDto>.Failure failure =>
        Results.Problem(
            detail: failure.Message,
            title: failure.Code,
            statusCode: MapErrorToStatus(failure.Code)
        ),
    
    _ => Results.StatusCode(500)
};
```

### Error Code Conventions

| Pattern | HTTP Status | Example | Meaning |
|---------|-------------|---------|---------|
| `{ENTITY}_NOT_FOUND` | 404 | `USER_NOT_FOUND` | Resource doesn't exist |
| `{OPERATION}_CONFLICT` | 409 | `CREATE_CONFLICT` | Business rule violated |
| `INVALID_{FIELD}` | 422 | `INVALID_EMAIL` | Validation failed |
| `UNAUTHORIZED_{OP}` | 403 | `UNAUTHORIZED_DELETE` | Permission denied |
| `INTERNAL_ERROR` | 500 | System error | Unexpected failure |

### Soft Delete

Records are marked deleted, not removed:

```csharp
// Active records only (automatic)
var items = await context.Items.Where(i => !i.IsDeleted).ToListAsync();

// Include deleted records
var allItems = await context.Items.IgnoreQueryFilters().ToListAsync();
```

### Audit Fields

Every entity has audit tracking:

```csharp
public abstract class BaseEntity
{
    public string Id { get; set; }
    public DateTime CreatedAt { get; set; }       // Auto-set
    public string CreatedBy { get; set; }         // User-set
    public DateTime? UpdatedAt { get; set; }      // Auto-set
    public string? UpdatedBy { get; set; }        // User-set
    public int Version { get; set; }              // Optimistic concurrency
    public bool IsDeleted { get; set; }           // Soft delete
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

### ID Generation

All IDs are 32-character lowercase GUIDs without hyphens:

```csharp
Id = Guid.NewGuid().ToString("N").ToLower();
// Example: "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6"
```

**Benefits:**
- Globally unique
- URL-friendly  
- Can be generated on client
- Database-independent

---

## API Usage

### Health Check Endpoints

The API provides three health check endpoints for monitoring and infrastructure integration:

#### GET /health

Overall health status including all registered health checks.

```bash
curl https://localhost:7288/health
```

**Response (200 OK):**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0415061",
  "entries": {
    "appdbcontext": {
      "status": "Healthy",
      "duration": "00:00:00.0371813",
      "description": null,
      "tags": ["ready", "database"]
    }
  }
}
```

#### GET /health/ready

Readiness probe for load balancers. Returns 200 only if the API can serve traffic (database connected, etc.).

```bash
curl https://localhost:7288/health/ready
```

**Use Case**: Kubernetes/Docker readiness probe, load balancer health checks

#### GET /health/live

Liveness probe. Always returns 200 if the process is alive (no health checks performed).

```bash
curl https://localhost:7288/health/live
```

**Response:**
```json
{
  "status": "Healthy",
  "message": "Process is alive"
}
```

**Use Case**: Kubernetes/Docker liveness probe

**Kubernetes Example:**
```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 5
```

⚠️ **Note**: All health endpoints allow anonymous access (no authentication required).

### Authentication

All **protected** endpoints require JWT Bearer token:

```http
Authorization: Bearer <your_jwt_token>
```

**Example with HttpClient:**
```csharp
var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);

var response = await client.GetAsync("https://localhost:7288/api/items/123");
```

### Response Format

#### Success (200 OK)
```json
{
  "id": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6",
  "name": "Sample Item",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

#### Validation Error (422 Unprocessable Entity)
```json
{
  "message": "Email format is invalid",
  "code": "VALIDATION_ERROR",
  "timestamp": "2026-07-08T08:30:00Z",
  "traceId": "trace-123",
  "errors": {
    "email": ["Invalid email format"],
    "age": ["Must be at least 18"]
  },
  "title": "Validation Error",
  "status": 422,
  "detail": "Email format is invalid"
}
```

#### Not Found (404)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "USER_NOT_FOUND",
  "status": 404,
  "detail": "User with ID 'abc123' was not found"
}
```

#### Conflict (409)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "USER_CONFLICT",
  "status": 409,
  "detail": "User with this email already exists"
}
```

### Using Scalar UI

The interactive API explorer at `/scalar/v1` lets you:
- ✅ Browse all endpoints
- ✅ View request/response schemas
- ✅ Try endpoints with real requests
- ✅ See authentication requirements
- ✅ Copy request examples

### Testing with cURL

```bash
# GET request
curl -H "Authorization: Bearer $TOKEN" \
  https://localhost:7288/api/items/123

# POST request
curl -X POST \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"New Item"}' \
  https://localhost:7288/api/items

# Include response headers
curl -i https://localhost:7288/api/items
```

---

## Data Model

### Entity Relationships

Entities are organized in layers:

```
Configuration Layer:
  ├─ Settings
  └─ Masters

Core Domain:
  ├─ Primary Entity
  ├─ Secondary Entity
  └─ Relationships

Activity Tracking:
  ├─ Requests
  ├─ Transitions
  └─ History

Compliance:
  ├─ Audit Logs
  └─ Evidence

Analytics:
  └─ Aggregated Metrics
```

### Querying

#### Include Related Data

```csharp
var item = await context.Items
    .Include(i => i.Category)
    .Include(i => i.Tags)
    .FirstOrDefaultAsync(i => i.Id == id);
```

#### Filter and Project

```csharp
var summary = await context.Items
    .Where(i => i.IsActive && i.Category.Name == "Electronics")
    .Select(i => new ItemSummary
    {
        Id = i.Id,
        Name = i.Name,
        Category = i.Category.Name
    })
    .ToListAsync();
```

#### Group and Aggregate

```csharp
var stats = await context.Items
    .GroupBy(i => i.Category.Name)
    .Select(g => new
    {
        Category = g.Key,
        Count = g.Count(),
        AveragePrice = g.Average(i => i.Price)
    })
    .ToListAsync();
```

### Hierarchical Data

For parent-child relationships (like categories with subcategories):

```csharp
// Get all children recursively
async Task<List<string>> GetHierarchy(string parentId)
{
    var ids = new List<string> { parentId };
    
    var children = await context.Items
        .Where(i => i.ParentId == parentId)
        .Select(i => i.Id)
        .ToListAsync();
    
    foreach (var childId in children)
    {
        ids.AddRange(await GetHierarchy(childId));
    }
    
    return ids;
}

// Get parent chain
var item = await context.Items
    .Include(i => i.Parent)
        .ThenInclude(p => p.Parent)
    .FirstOrDefaultAsync(i => i.Id == id);

var chain = new List<string>();
var current = item;
while (current != null)
{
    chain.Insert(0, current.Name);
    current = current.Parent;
}
```

---

## Development Guide

### Creating an Endpoint

#### 1. Define Request/Response Models

```csharp
// Models.cs
public record CreateItemRequest(string Name, string Description);
public record ItemDto(string Id, string Name, string Description, DateTime CreatedAt);
public record CreatedItemDto(string Id, string Name);
```

#### 2. Create Query/Command

```csharp
// Handlers.cs
public record GetItemByIdQuery(string Id) : IRequest<Result<ItemDto>>;
public record CreateItemCommand(string Name, string Description) : IRequest<Result<CreatedItemDto>>;
```

#### 3. Implement Handler

```csharp
public class GetItemByIdQueryHandler : ResultQueryHandler<GetItemByIdQuery, ItemDto>
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    
    public GetItemByIdQueryHandler(
        AppDbContext context,
        IMapper mapper,
        ILogger<GetItemByIdQueryHandler> logger) : base(logger)
    {
        _context = context;
        _mapper = mapper;
    }
    
    public override async Task<Result<ItemDto>> HandleAsync(
        GetItemByIdQuery request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
            return Failure("INVALID_ID", "ID is required");
        
        var item = await _context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct);
        
        if (item == null)
            return Failure("ITEM_NOT_FOUND", $"Item '{request.Id}' not found");
        
        return Success(_mapper.Map<ItemDto>(item));
    }
}

public class CreateItemCommandHandler : ResultCommandHandler<CreateItemCommand, CreatedItemDto>
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    
    public CreateItemCommandHandler(
        AppDbContext context,
        IMapper mapper,
        ILogger<CreateItemCommandHandler> logger) : base(logger)
    {
        _context = context;
        _mapper = mapper;
    }
    
    public override async Task<Result<CreatedItemDto>> HandleAsync(
        CreateItemCommand request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Failure("INVALID_NAME", "Name is required");
        
        var existing = await _context.Items
            .FirstOrDefaultAsync(i => i.Name == request.Name, ct);
        
        if (existing != null)
            return Failure("ITEM_CONFLICT", "Item with this name already exists");
        
        var item = new Item
        {
            Id = Guid.NewGuid().ToString("N").ToLower(),
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Items.Add(item);
        await _context.SaveChangesAsync(ct);
        
        return Success(_mapper.Map<CreatedItemDto>(item));
    }
}
```

#### 4. Create Module

```csharp
public class ItemModule : IModule
{
    public IServiceCollection RegisterModule(IServiceCollection services)
    {
        // Handlers auto-registered by MediatR
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/items")
            .WithName("Items")
            .WithTags("Items");

        group.MapGet("{id}", GetItemById)
            .WithName("GetItemById")
            .Produces<ItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Get item by ID")
            .WithDescription("Retrieves a single item by its ID.");

        group.MapPost("/", CreateItem)
            .WithName("CreateItem")
            .Produces<CreatedItemDto>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .WithSummary("Create item")
            .WithDescription("Creates a new item.");

        return endpoints;
    }

    private static async Task<IResult> GetItemById(string id, IMediator mediator)
    {
        var result = await mediator.Send(new GetItemByIdQuery(id));
        return result.ToHttpResponse();
    }

    private static async Task<IResult> CreateItem(CreateItemRequest request, IMediator mediator)
    {
        var result = await mediator.Send(new CreateItemCommand(request.Name, request.Description));

        return result switch
        {
            Result<CreatedItemDto>.Success success =>
                Results.Created($"/items/{success.Data.Id}", success.Data),

            Result<CreatedItemDto>.Failure failure =>
                Results.Problem(
                    detail: failure.Message,
                    title: failure.Code,
                    statusCode: MapErrorToStatus(failure.Code),
                    extensions: failure.Details != null ?
                        new Dictionary<string, object?> { { "errors", failure.Details } } :
                        null
                ),

            _ => Results.StatusCode(500)
        };
    }

    private static int MapErrorToStatus(string code) => code switch
    {
        var c when c.EndsWith("_NOT_FOUND") => StatusCodes.Status404NotFound,
        var c when c.EndsWith("_CONFLICT") => StatusCodes.Status409Conflict,
        var c when c.StartsWith("INVALID_") => StatusCodes.Status422UnprocessableEntity,
        var c when c.StartsWith("UNAUTHORIZED_") => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };
}
```

**Important Notes:**
- ✅ **Do NOT call `WithOpenApi()`** - It's deprecated in .NET 10. OpenAPI is configured globally in `Program.cs` using `AddOpenApi()`.
- ✅ **Use `Produces()`** for response type documentation
- ✅ **Use `WithSummary()` and `WithDescription()`** for endpoint documentation
- The global OpenAPI configuration in `Program.cs` automatically discovers all endpoints and their metadata

### Adding Validation

Use FluentValidation:

```csharp
public class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
{
    public CreateItemCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
    }
}
```

Validation runs automatically via `ValidationBehavior` in the MediatR pipeline.

### Structured Logging

```csharp
// Available in handler base classes
Logger.LogDebug("Processing {Operation} for {EntityId}", "Create", entityId);
Logger.LogInformation("Item created: {ItemId}", item.Id);
Logger.LogWarning("Item not found: {ItemId}", itemId);
Logger.LogError(ex, "Database error processing {Operation}", "CreateItem");
```

**Log Levels:**
- **Debug** - Development diagnostics
- **Information** - Important business events
- **Warning** - Potential issues
- **Error** - Recoverable errors
- **Fatal** - Unrecoverable failures

### Exception Handling

#### When to Use Result Pattern
```csharp
// Expected business outcome
if (item == null)
    return Failure("ITEM_NOT_FOUND", "Item not found");
```

#### When to Throw Exceptions
```csharp
// Truly unexpected error
try
{
    await _context.SaveChangesAsync();
}
catch (PostgresException ex)
{
    throw new ApplicationException("Database connection failed", ex);
}
```

Exceptions are caught by `ExceptionHandlingMiddleware` and return 500 responses.

### Request Tracing

Correlation IDs automatically track requests:

```csharp
// Middleware adds X-Correlation-ID to all responses
// All logs automatically include correlationId

// Explicitly use in code:
var correlationId = context.Items["CorrelationId"];
Logger.LogInformation("Processing {CorrelationId}", correlationId);
```

---

## Testing Guide

### Project Structure

```
ForgeKit.Api.Tests/
├── Unit/
│   ├── Handlers/
│   ├── Services/
│   └── Validators/
├── Integration/
│   ├── Endpoints/
│   └── Database/
└── Samples/
```

### Unit Test Example

```csharp
[Fact]
public async Task Handle_WhenItemExists_ReturnsSuccess()
{
    // Arrange
    var handler = new GetItemByIdQueryHandler(_mockContext, _mockMapper, _mockLogger);
    var query = new GetItemByIdQuery("item-123");
    
    var item = new Item { Id = "item-123", Name = "Test Item" };
    _mockContext.Items
        .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Item, bool>>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(item);

    _mockMapper.Setup(m => m.Map<ItemDto>(item))
        .Returns(new ItemDto("item-123", "Test Item", "", DateTime.UtcNow));

    // Act
    var result = await handler.HandleAsync(query, CancellationToken.None);

    // Assert
    Assert.IsType<Result<ItemDto>.Success>(result);
    var success = (Result<ItemDto>.Success)result;
    Assert.Equal("item-123", success.Data.Id);
}

[Fact]
public async Task Handle_WhenItemNotFound_ReturnsFailure()
{
    // Arrange
    var handler = new GetItemByIdQueryHandler(_mockContext, _mockMapper, _mockLogger);
    var query = new GetItemByIdQuery("non-existent");

    _mockContext.Items
        .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Item, bool>>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((Item)null);

    // Act
    var result = await handler.HandleAsync(query, CancellationToken.None);

    // Assert
    Assert.IsType<Result<ItemDto>.Failure>(result);
    var failure = (Result<ItemDto>.Failure)result;
    Assert.Equal("ITEM_NOT_FOUND", failure.Code);
}
```

### Integration Test Example

```csharp
public class ItemEndpointsTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    public ItemEndpointsTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase("TestDb"));
                });
            });
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Fact]
    public async Task GetItem_WhenExists_Returns200()
    {
        // Arrange
        var item = new Item { Id = "test-123", Name = "Test" };
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Items.Add(item);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/items/test-123");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetItem_WhenNotFound_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/items/non-existent");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
```

**Note:** This project uses **Shouldly** for assertions.

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific project
dotnet test ForgeKit.Api.Tests

# Run specific test class
dotnet test --filter FullyQualifiedName~ItemEndpointsTests

# Verbose output
dotnet test --verbosity detailed
```

---

## Troubleshooting

### Database Issues

#### Q: "Cannot open database" error on startup

**Symptoms:** Application fails to start with database connection error.

**Causes:**
- Connection string is incorrect
- Database doesn't exist
- PostgreSQL is not running
- Network connectivity issues
- Incorrect credentials

**Solutions:**
1. Verify connection string in `appsettings.Development.json`
2. Check PostgreSQL is running:
   ```bash
   # macOS/Homebrew
   brew services list | grep postgresql
   
   # Check if accessible
   pg_isready -h localhost -p 5432
   ```
3. Test connection with `psql` or your PostgreSQL client
4. Create database if missing:
   ```bash
   dotnet ef database update --project ForgeKit.Api.Migrations.Postgres --startup-project ForgeKit.Api.Migrations.Postgres --context AppDbContext
   dotnet ef database update --project ForgeKit.Api.Migrations.Postgres --startup-project ForgeKit.Api.Migrations.Postgres --context BetterAuthDbContext
   ```
5. Check firewall rules allow PostgreSQL port (5432)

---

#### Q: How do I add a database migration?

**A:**
```bash
cd api
dotnet ef migrations add DescriptionOfChange --project ForgeKit.Api.Migrations.Sqlite --startup-project ForgeKit.Api.Migrations.Sqlite --context AppDbContext --output-dir Migrations/App
```

Repeat the command for every supported provider project, and use `Migrations/Auth` with `BetterAuthDbContext` when the authentication model changes. Always review generated migration code before applying.

---

#### Q: "Cannot insert duplicate key" error

**Cause:** Trying to insert entity with existing ID or unique constraint violation.

**Solution:**
```csharp
// Check existence before inserting
var existing = await context.Items.FindAsync(id);
if (existing != null)
    return Failure("ITEM_EXISTS", "Item already exists");

// Or use Guid for guaranteed uniqueness
var newId = Guid.NewGuid().ToString("N").ToLower();
```

---

#### Q: Soft-deleted entities appearing in queries

**Cause:** Query filter not applied or explicitly disabled.

**Solutions:**
```csharp
// Problem: IgnoreQueryFilters bypasses soft-delete filter
var all = await context.Items.IgnoreQueryFilters().ToListAsync();

// Solution 1: Remove IgnoreQueryFilters
var active = await context.Items.ToListAsync();  // Only non-deleted

// Solution 2: Explicitly filter if you need IgnoreQueryFilters for other reasons
var active = await context.Items
    .IgnoreQueryFilters()
    .Where(i => !i.IsDeleted)
    .ToListAsync();
```

---

#### Q: "DbUpdateConcurrencyException" on SaveChanges

**Cause:** Entity was modified by another user between read and write.

**Solutions:**
```csharp
// Strategy 1: Reload and retry
try
{
    await context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    await context.Entry(entity).ReloadAsync();
    // Re-apply changes or notify user
}

// Strategy 2: Use transactions with isolation level
using var transaction = await context.Database.BeginTransactionAsync(
    System.Data.IsolationLevel.RepeatableRead);
```

---

### Authentication & Authorization Issues

#### Q: "401 Unauthorized" on protected endpoint

**Causes:**
- JWT token missing or malformed
- Token expired
- Issuer/Audience mismatch
- JWKS endpoint unreachable

**Solutions:**
1. Check token is present in Authorization header:
   ```bash
   curl -H "Authorization: <bearer-token>" https://localhost:5001/api/resource
   ```
2. Decode JWT token (use jwt.io) and verify:
   - `iss` (issuer) matches `JwtData:Issuer` in appsettings
   - `aud` (audience) matches `JwtData:Audience`
   - `exp` (expiration) is in the future
3. Test JWKS endpoint:
   ```bash
   curl https://your-auth-server/api/auth/jwks
   ```
4. Enable debug logging:
   ```json
   "Serilog": {
     "MinimumLevel": {
       "Default": "Debug",
       "Override": {
         "Microsoft.AspNetCore.Authentication": "Debug"
       }
     }
   }
   ```

---

#### Q: How do I call an endpoint with authentication?

**A:**
```csharp
var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", myJwtToken);

var response = await client.GetAsync("https://localhost:7288/api/items");
```

---

#### Q: "403 Forbidden" despite valid JWT

**Cause:** User is authenticated but lacks required permissions/roles.

**Solution:**
1. Check JWT claims include required roles:
   ```json
   {
     "sub": "user123",
     "roles": ["Admin", "User"]
   }
   ```
2. Verify endpoint authorization requirements:
   ```csharp
   group.MapDelete("/{id}", DeleteItem)
       .RequireAuthorization("AdminOnly");  // Check policy name
   ```
3. Ensure authorization policies are registered:
   ```csharp
   builder.Services.AddAuthorization(options =>
   {
       options.AddPolicy("AdminOnly", policy => 
           policy.RequireRole("Admin"));
   });
   ```

---

### Validation Issues

#### Q: Validation not triggered for my command

**Cause:** Validator not discovered or not registered.

**Solutions:**
1. Ensure validator is in same assembly as `Program.cs`
2. Check validator class name: `{CommandName}Validator`
3. Verify validator inherits from `AbstractValidator<T>`:
   ```csharp
   public class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
   ```
4. Check `AddValidatorsFromAssembly` is called in Program.cs:
   ```csharp
   builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
   ```

---

#### Q: How do I include field-level errors in a response?

**A:**
```csharp
var errors = new Dictionary<string, string[]>
{
    { "email", new[] { "Invalid format" } },
    { "age", new[] { "Must be at least 18" } }
};

return Failure("VALIDATION_ERROR", "Multiple validation errors", errors);
```

**Note:** FluentValidation automatically populates field errors. Only use manual errors for custom business logic.

---

#### Q: Getting validation errors in production but not development

**Cause:** Environment-specific data or validation rules.

**Solutions:**
1. Check validators don't reference environment-specific data
2. Use same test data across environments
3. Review logs for detailed validation errors:
   ```bash
   grep "VALIDATION_ERROR" logs/log-*.txt
   ```

---

### Module & Endpoint Issues

#### Q: My new module endpoints not showing in Swagger

**Causes:**
- Module not implementing `IModule` interface
- Module not in discovered namespace
- Endpoints not tagged correctly

**Solutions:**
1. Verify module implements `IModule`:
   ```csharp
   public class MyModule : IModule
   ```
2. Check module is in `ForgeKit.Api/Modules/` or subdirectory
3. Add OpenAPI metadata:
   ```csharp
   group.MapGet("/", GetItems)
       .WithOpenApi()  // Add this
       .WithTags("MyModule");
   ```
4. Restart application (modules discovered at startup)

---

#### Q: Sample endpoints visible in production

**Cause:** Environment not set to "Production" or ISampleModule not applied.

**Solutions:**
1. Set environment variable:
   ```bash
   # Windows
   $env:ASPNETCORE_ENVIRONMENT="Production"
   
   # Linux
   export ASPNETCORE_ENVIRONMENT=Production
   ```
2. Verify environment in logs (first line of output)
3. Ensure sample modules implement `ISampleModule`:
   ```csharp
   public class SampleResourceModule : ISampleModule  // Not IModule
   ```

---

#### Q: "Duplicate endpoint name" error on startup

**Cause:** Multiple endpoints with same `.WithName()` value.

**Solution:**
1. Ensure each endpoint has unique name:
   ```csharp
   group.MapGet("/{id}", GetById).WithName("GetItemById");  // Unique
   group.MapGet("/", GetAll).WithName("GetItems");  // Unique
   ```
2. Follow naming convention: `{Verb}{EntityName}[ById]`

---

### Logging Issues

#### Q: Logs not appearing in console/file

**Causes:**
- Log level too high (filtering out messages)
- Sink misconfigured
- File permissions issue

**Solutions:**
1. Lower log level temporarily:
   ```json
   "Serilog": {
     "MinimumLevel": {
       "Default": "Debug"
     }
   }
   ```
2. Test console sink first:
   ```json
   "WriteTo": [
     { "Name": "Console" }
   ]
   ```
3. Check file path and permissions:
   ```bash
   # Ensure logs directory exists and is writable
   mkdir logs
   chmod 777 logs  # Linux
   ```
4. Verify Serilog configuration is valid JSON (use JSON validator)

---

#### Q: Correlation IDs not appearing in logs

**Cause:** CorrelationIdMiddleware not registered or LogContext not enriched.

**Solutions:**
1. Verify middleware registration in Program.cs:
   ```csharp
   app.UseMiddleware<CorrelationIdMiddleware>();
   ```
2. Check Serilog enrichment:
   ```json
   "Enrich": ["FromLogContext"]
   ```
3. Test by sending custom correlation ID:
   ```bash
   curl -H "X-Correlation-ID: test-123" https://localhost:5001/api/resource
   ```

---

#### Q: How are correlation IDs used?

**A:**
```csharp
// Middleware automatically adds to all responses
// Response includes: X-Correlation-ID: <unique-id>

// All logs automatically include correlationId
Logger.LogInformation("Processing item");
// Output: [...] correlationId=abc123 Processing item
```

**Usage for debugging:**
1. Get correlation ID from error response
2. Search logs: `grep "abc123" logs/log-*.txt`
3. See complete request flow

---

### Performance Issues

#### Q: Slow query performance

**Causes:**
- Missing indexes
- N+1 query problem
- Eager loading unnecessary data
- Not using AsNoTracking for read-only queries

**Solutions:**
1. Add indexes for frequently queried columns:
   ```csharp
   builder.HasIndex(e => e.CreatedAt);
   builder.HasIndex(e => new { e.CustomerId, e.Status });
   ```
2. Use `Include()` to avoid N+1 queries:
   ```csharp
   // Bad: N+1 queries
   var orders = await context.Orders.ToListAsync();
   foreach (var order in orders)
       var items = order.Items;  // Separate query per order!
   
   // Good: Single query
   var orders = await context.Orders
       .Include(o => o.Items)
       .ToListAsync();
   ```
3. Use `AsNoTracking()` for read-only operations:
   ```csharp
   var items = await context.Items
       .AsNoTracking()  // Faster, no change tracking overhead
       .ToListAsync();
   ```
4. Enable SQL logging to see generated queries:
   ```json
   "Microsoft.EntityFrameworkCore.Database.Command": "Information"
   ```

---

#### Q: High memory usage

**Causes:**
- Loading too much data at once
- Change tracker holding references
- Not disposing DbContext

**Solutions:**
1. Use pagination:
   ```csharp
   var page = await context.Items
       .Skip(pageNumber * pageSize)
       .Take(pageSize)
       .ToListAsync();
   ```
2. Use projections (select only needed fields):
   ```csharp
   var dtos = await context.Orders
       .Select(o => new OrderDto(o.Id, o.CustomerId, o.Total))
       .ToListAsync();
   ```
3. Dispose contexts explicitly in long-running operations:
   ```csharp
   using (var context = new AppDbContext(options))
   {
       // Use context
   } // Disposed here
   ```

---

### Configuration Issues

#### Q: Configuration changes not taking effect

**Causes:**
- Wrong environment file
- Configuration cached
- Typos in JSON
- Environment variable not set before startup

**Solutions:**
1. Check active environment:
   ```bash
   echo $env:ASPNETCORE_ENVIRONMENT  # Windows
   echo $ASPNETCORE_ENVIRONMENT      # Linux
   ```
2. Ensure correct file: `appsettings.{Environment}.json`
3. Validate JSON syntax (use online validator)
4. Restart application (configuration read at startup)
5. For environment variables, use double underscore:
   ```bash
   Database__Provider="Postgres"
   ConnectionStrings__Postgres="Host=..."
   ```

---

#### Q: "Configuration key not found" error

**Cause:** Missing required configuration setting.

**Solution:**
```csharp
// Add validation at startup
var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString(provider);
if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("Database connection string not configured!");
```

---

### Testing Issues

#### Q: Tests fail in CI/CD but pass locally

**Causes:**
- Environment-specific configuration
- Database state differences
- Time zone differences
- Parallel test execution conflicts

**Solutions:**
1. Use `TestWebApplicationFactory` for isolated testing:
   ```csharp
   var factory = new TestWebApplicationFactory();
   var client = factory.CreateClient();
   ```
2. Use unique database per test:
   ```csharp
   var options = new DbContextOptionsBuilder<AppDbContext>()
       .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
       .Options;
   ```
3. Use `DateTime.UtcNow` consistently (avoid local time)
4. Disable parallel execution if needed:
   ```csharp
   [Collection("Sequential")]
   public class MyTests { }
   ```

---

#### Q: Integration tests hitting production database

**Cause:** Not using `TestWebApplicationFactory` or wrong connection string.

**Solution:**
```csharp
// Use TestWebApplicationFactory which uses in-memory database
public class MyIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MyIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
}
```

---

### General Issues

#### Q: When should I use Result vs Exception?

**A:**

| Scenario | Use |
|----------|-----|
| Not found | Result.Failure |
| Validation failed | Result.Failure |
| Business rule violated | Result.Failure |
| Permission denied | Result.Failure |
| Database connection failed | Exception |
| Null reference | Exception |
| Configuration error | Exception |
| Unexpected error | Exception |

**Rule of thumb:** Expected errors → Result.Failure, Unexpected errors → Exception

---

#### Q: Application starts but endpoints return 404

**Causes:**
- Routing misconfigured
- Module not discovered
- Wrong URL path

**Solutions:**
1. Check endpoint mapping in module:
   ```csharp
   var group = endpoints.MapGroup("/api/v1/items");  // Check prefix
   group.MapGet("/{id}", GetById);  // Full path: /api/v1/items/{id}
   ```
2. Test Scalar UI: `/scalar/v1` (shows all endpoints)
3. Check application is listening on correct port:
   ```bash
   netstat -an | findstr :5001
   ```

---

### Getting Help

When reporting issues, include:
1. **Correlation ID / Trace ID** from error response
2. **Full error message** and stack trace
3. **Request details** (method, URL, body)
4. **Expected vs actual behavior**
5. **Environment** (Development, Staging, Production)
6. **Relevant logs** (search by correlation ID)

**Example:**
```
Issue: Getting 500 error on POST /api/v1/orders
Correlation ID: 3fa85f64-5717-4562-b3fc-2c963f66afa6
Environment: Development
Request body: { "customerId": "123", "items": [] }
Expected: 201 Created
Actual: 500 Internal Server Error
Logs: See attached log-2026-02-11.txt (line 4521)
```

---

## Resources

### Internal Documentation

- [Configuration Guide](./CONFIGURATION_GUIDE.md) - Complete configuration reference
- [Extending the API](./EXTENDING_THE_API.md) - How to add modules, endpoints, and features
- [Result Pattern Guide](./RESULT_PATTERN_GUIDE.md) - Comprehensive Result<T> usage
- [FluentValidation Guide](./FLUENT_VALIDATION_GUIDE.md) - Validation patterns and examples
- [Exception Handling](./EXCEPTION_HANDLING_GUIDE.md) - Exception patterns
- [XML Documentation Guide](./XML_DOCUMENTATION_GUIDE.md) - XML comments and OpenAPI documentation standards
- [Glossary](./GLOSSARY.md) - Terms and concepts reference
- [Commit Convention](./COMMIT_CONVENTION.md) - Git commit standards
- [Logging Guide](./logging.md) - Logging best practices
- [Architecture Decision Records](./adr/) - ADR documentation

### External Resources

- [Railway-Oriented Programming](https://fsharpforfunandprofit.com/rop/) - ROP background
- [Entity Framework Core](https://docs.microsoft.com/ef/core/) - ORM documentation
- [MediatR](https://github.com/jbogard/MediatR) - CQRS pattern library
- [FluentValidation](https://fluentvalidation.net/) - Validation framework
- [Serilog](https://serilog.net/) - Logging framework
- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)

### Tools

- **Scalar UI** - Built-in interactive API explorer
- **Postman** - API client (import OpenAPI JSON)
- **VS Code REST Client** - Test with `.http` files
- **curl** - Command-line testing

---

**Version**: 1.0  
**Last Updated**: February 2026  
**Maintained by**: Development Team
