# Extending the API Guide

A comprehensive guide for developers on how to extend and customize the ForgeKit API with new features, modules, and functionality.

## Table of Contents

1. [Overview](#overview)
2. [Before You Start](#before-you-start)
3. [Adding a New Module](#adding-a-new-module)
4. [Adding New Endpoints](#adding-new-endpoints)
5. [Creating Commands & Queries](#creating-commands--queries)
6. [Adding Validators](#adding-validators)
7. [Creating Domain Services](#creating-domain-services)
8. [Adding New Entities](#adding-new-entities)
9. [Adding Custom Health Checks](#adding-custom-health-checks)
10. [Customizing Error Handling](#customizing-error-handling)
11. [Extending Middleware](#extending-middleware)
12. [Testing Your Extensions](#testing-your-extensions)
13. [Best Practices](#best-practices)

---

## Overview

The ForgeKit API is designed to be **easily extensible** through well-defined patterns. This guide shows you how to add new functionality while maintaining architectural consistency.

### Extension Points

| What | How | Complexity |
|------|-----|------------|
| **New Feature/Module** | Implement `IModule` | ⭐⭐ Medium |
| **New Endpoint** | Add to existing module | ⭐ Easy |
| **New Command/Query** | Create handler + request | ⭐⭐ Medium |
| **New Validator** | Create `AbstractValidator<T>` | ⭐ Easy |
| **New Entity** | Inherit `BaseEntity` | ⭐⭐ Medium |
| **Custom Exception** | Inherit `AppException` | ⭐ Easy |
| **New Middleware** | Implement middleware pattern | ⭐⭐⭐ Advanced |
| **Custom Behavior** | Implement `IPipelineBehavior<,>` | ⭐⭐⭐ Advanced |

---

## Before You Start

### Decision Tree: Should I Create a New Module?

```
Is this a new feature or domain concept?
│
├─ YES ─► Does it have ≥3 endpoints?
│         │
│         ├─ YES ─► Create new module (IModule)
│         │
│         └─ NO ──► Add to existing related module
│
└─ NO ──► Are you modifying existing functionality?
          │
          ├─ YES ─► Extend existing module
          │
          └─ NO ──► Reconsider if this belongs in API layer
```

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Module | `{Feature}Module` | `OrderModule`, `CustomerModule` |
| Command | `{Verb}{Entity}Command` | `CreateOrderCommand`, `UpdateCustomerCommand` |
| Query | `{Verb}{Entity}Query` or `Get{Entity}Query` | `GetOrderByIdQuery`, `ListCustomersQuery` |
| Handler | `{Request}Handler` | `CreateOrderCommandHandler` |
| Validator | `{Request}Validator` | `CreateOrderCommandValidator` |
| Entity | `{Name}` (singular) | `Order`, `Customer` |
| DTO | `{Entity}Dto` | `OrderDto`, `CustomerDto` |

---

## Adding a New Module

### Step 1: Define the Module Structure

Create a directory structure:

```
ForgeKit.Api/Modules/
  └── Orders/              # Your new module
      ├── OrderModule.cs   # Module registration
      ├── Commands/
      │   ├── CreateOrderCommand.cs
      │   └── CreateOrderCommandHandler.cs
      ├── Queries/
      │   ├── GetOrderByIdQuery.cs
      │   └── GetOrderByIdQueryHandler.cs
      ├── Validators/
      │   └── CreateOrderCommandValidator.cs
      └── Dtos/
          └── OrderDto.cs
```

### Step 2: Create the Module Class

```csharp
// ForgeKit.Api/Modules/Orders/OrderModule.cs
using Api.Interfaces;

namespace Api.Modules.Orders;

public class OrderModule : IModule
{
    public IServiceCollection RegisterModule(IServiceCollection services)
    {
        // Register module-specific services if needed
        // Most handlers are auto-registered by MediatR
        
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/orders")
            .WithName("Orders")
            .WithTags("Orders")
            .WithOpenApi();

        // Define endpoints
        group.MapGet("/{id}", GetOrderById)
            .WithName("GetOrderById")
            .Produces<OrderDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Get order by ID");

        group.MapPost("/", CreateOrder)
            .WithName("CreateOrder")
            .Produces<OrderDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .WithSummary("Create new order");

        return endpoints;
    }

    // Endpoint handlers
    private static async Task<IResult> GetOrderById(
        string id,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new GetOrderByIdQuery(id);
        var result = await mediator.Send(query, ct);

        return result switch
        {
            Result<OrderDto>.Success success => Results.Ok(success.Data),
            Result<OrderDto>.Failure failure => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Order Not Found",
                detail: failure.Message),
            _ => throw new InvalidOperationException("Unexpected result type")
        };
    }

    private static async Task<IResult> CreateOrder(
        CreateOrderCommand command,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

        return result switch
        {
            Result<OrderDto>.Success success => Results.Created(
                $"/api/v1/orders/{success.Data.Id}",
                success.Data),
            Result<OrderDto>.Failure failure => Results.BadRequest(
                new { error = failure.Code, message = failure.Message }),
            _ => throw new InvalidOperationException("Unexpected result type")
        };
    }
}
```

### Step 3: Module Auto-Discovery

**Good news:** Your module is automatically discovered! No need to manually register it.

The `ModuleExtensions.RegisterModules()` method uses reflection to find all `IModule` implementations.

**Location:** `ForgeKit.Api/Extensions/ModuleExtension.cs`

---

## Adding New Endpoints

### Minimal Endpoint

```csharp
// In your module's MapEndpoints method
group.MapGet("/{id}/status", GetOrderStatus)
    .WithName("GetOrderStatus")
    .Produces<string>(StatusCodes.Status200OK)
    .WithSummary("Get order status");

private static async Task<IResult> GetOrderStatus(
    string id,
    IMediator mediator,
    CancellationToken ct)
{
    var query = new GetOrderStatusQuery(id);
    var result = await mediator.Send(query, ct);

    return result switch
    {
        Result<string>.Success success => Results.Ok(success.Data),
        Result<string>.Failure failure => Results.NotFound(),
        _ => throw new InvalidOperationException()
    };
}
```

### Endpoint with Authentication

```csharp
group.MapPost("/", CreateOrder)
    .RequireAuthorization()  // Add authentication requirement
    .WithName("CreateOrder");
```

### Endpoint with Authorization Policy

```csharp
group.MapDelete("/{id}", DeleteOrder)
    .RequireAuthorization("AdminOnly")  // Custom policy
    .WithName("DeleteOrder");
```

### Endpoint with Custom Response Headers

```csharp
private static async Task<IResult> CreateOrder(
    CreateOrderCommand command,
    IMediator mediator,
    HttpContext httpContext,
    CancellationToken ct)
{
    var result = await mediator.Send(command, ct);

    return result switch
    {
        Result<OrderDto>.Success success => 
        {
            // Add custom header
            httpContext.Response.Headers.Append("X-Order-Id", success.Data.Id);
            return Results.Created($"/api/v1/orders/{success.Data.Id}", success.Data);
        },
        // ... failure cases
    };
}
```

---

## Creating Commands & Queries

### Command Pattern (Write Operation)

#### 1. Define the Command

```csharp
// ForgeKit.Api/Modules/Orders/Commands/CreateOrderCommand.cs
using MediatR;

namespace Api.Modules.Orders.Commands;

public record CreateOrderCommand(
    string CustomerId,
    List<OrderItemDto> Items,
    decimal TotalAmount
) : IRequest<Result<OrderDto>>;

public record OrderItemDto(string ProductId, int Quantity, decimal Price);
```

#### 2. Create the Handler

```csharp
// ForgeKit.Api/Modules/Orders/Commands/CreateOrderCommandHandler.cs
using Api.Handlers;

namespace Api.Modules.Orders.Commands;

public class CreateOrderCommandHandler : ResultCommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateOrderCommandHandler> logger) : base(logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public override async Task<Result<OrderDto>> HandleAsync(
        CreateOrderCommand request,
        CancellationToken ct)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        // Validate business rules
        if (request.Items.Count == 0)
            return Failure("EMPTY_ORDER", "Order must contain at least one item");

        // Create entity
        var order = new Order
        {
            Id = Guid.NewGuid().ToString("N").ToLower(),
            CustomerId = request.CustomerId,
            TotalAmount = request.TotalAmount,
            Status = OrderStatus.Pending,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList()
        };

        // Save to database
        _unitOfWork.Orders.Add(order);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderId} created successfully", order.Id);

        // Return DTO
        var dto = new OrderDto(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.TotalAmount,
            order.CreatedAt);

        return Success(dto);
    }
}
```

### Query Pattern (Read Operation)

#### 1. Define the Query

```csharp
// ForgeKit.Api/Modules/Orders/Queries/GetOrderByIdQuery.cs
using MediatR;

namespace Api.Modules.Orders.Queries;

public record GetOrderByIdQuery(string Id) : IRequest<Result<OrderDto>>;
```

#### 2. Create the Handler

```csharp
// ForgeKit.Api/Modules/Orders/Queries/GetOrderByIdQueryHandler.cs
using Api.Handlers;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.Orders.Queries;

public class GetOrderByIdQueryHandler : ResultQueryHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly AppDbContext _context;
    private readonly ILogger<GetOrderByIdQueryHandler> _logger;

    public GetOrderByIdQueryHandler(
        AppDbContext context,
        ILogger<GetOrderByIdQueryHandler> logger) : base(logger)
    {
        _context = context;
        _logger = logger;
    }

    public override async Task<Result<OrderDto>> HandleAsync(
        GetOrderByIdQuery request,
        CancellationToken ct)
    {
        _logger.LogDebug("Fetching order {OrderId}", request.Id);

        var order = await _context.Orders
            .AsNoTracking()  // Read-only query
            .Include(o => o.Items)  // Load related items
            .FirstOrDefaultAsync(o => o.Id == request.Id, ct);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", request.Id);
            return Failure("ORDER_NOT_FOUND", $"Order '{request.Id}' not found");
        }

        var dto = new OrderDto(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.TotalAmount,
            order.CreatedAt);

        return Success(dto);
    }
}
```

---

## Adding Validators

### Creating a Validator

```csharp
// ForgeKit.Api/Modules/Orders/Validators/CreateOrderCommandValidator.cs
using FluentValidation;

namespace Api.Modules.Orders.Validators;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required")
            .Length(24)
            .WithMessage("Customer ID must be 24 characters");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Order must contain at least one item");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0)
            .WithMessage("Total amount must be greater than zero")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Total amount cannot exceed $1,000,000");

        // Nested validation
        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemValidator());
    }
}

public class OrderItemValidator : AbstractValidator<OrderItemDto>
{
    public OrderItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be at least 1")
            .LessThanOrEqualTo(1000)
            .WithMessage("Quantity cannot exceed 1000");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Price cannot be negative");
    }
}
```

### Advanced Validation Scenarios

#### Async Validation (Database Lookup)

```csharp
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    private readonly AppDbContext _context;

    public CreateOrderCommandValidator(AppDbContext context)
    {
        _context = context;

        RuleFor(x => x.CustomerId)
            .MustAsync(CustomerExists)
            .WithMessage("Customer does not exist");
    }

    private async Task<bool> CustomerExists(string customerId, CancellationToken ct)
    {
        return await _context.Customers
            .AnyAsync(c => c.Id == customerId, ct);
    }
}
```

#### Cross-Field Validation

```csharp
RuleFor(x => x)
    .Must(order => order.TotalAmount == order.Items.Sum(i => i.Price * i.Quantity))
    .WithMessage("Total amount does not match sum of items");
```

---

## Creating Domain Services

### When to Create a Domain Service

Create a domain service when:
- ✅ Logic spans multiple entities
- ✅ Business rule doesn't fit naturally in a single entity
- ✅ Need to coordinate between multiple aggregates

### Example: Order Fulfillment Service

```csharp
// ForgeKit.Api/Domain/Services/OrderFulfillmentService.cs
namespace Api.Domain.Services;

public interface IOrderFulfillmentService
{
    Task<bool> CanFulfillOrderAsync(string orderId, CancellationToken ct = default);
    Task<Order> FulfillOrderAsync(string orderId, CancellationToken ct = default);
}

public class OrderFulfillmentService : IOrderFulfillmentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<OrderFulfillmentService> _logger;

    public OrderFulfillmentService(
        AppDbContext context,
        ILogger<OrderFulfillmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> CanFulfillOrderAsync(string orderId, CancellationToken ct = default)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null || order.Status != OrderStatus.Pending)
            return false;

        // Check inventory for all items
        foreach (var item in order.Items)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == item.ProductId, ct);

            if (product == null || product.Stock < item.Quantity)
                return false;
        }

        return true;
    }

    public async Task<Order> FulfillOrderAsync(string orderId, CancellationToken ct = default)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
            throw new NotFoundException($"Order '{orderId}' not found");

        if (order.Status != OrderStatus.Pending)
            throw new BusinessLogicException("Order is not in pending status");

        // Reduce inventory
        foreach (var item in order.Items)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == item.ProductId, ct);

            if (product == null)
                throw new NotFoundException($"Product '{item.ProductId}' not found");

            if (product.Stock < item.Quantity)
                throw new BusinessLogicException($"Insufficient stock for product '{product.Name}'");

            product.Stock -= item.Quantity;
        }

        // Update order status
        order.Status = OrderStatus.Fulfilled;
        order.FulfilledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderId} fulfilled successfully", orderId);

        return order;
    }
}
```

### Registering the Service

```csharp
// ForgeKit.Api/Extensions/ServiceCollectionExtensions.cs (or similar)
public static IServiceCollection RegisterDomainServices(this IServiceCollection services)
{
    services.AddScoped<ISoftDeleteDomainService, SoftDeleteDomainService>();
    services.AddScoped<IOrderFulfillmentService, OrderFulfillmentService>(); // Add this
    
    return services;
}
```

---

## Adding New Entities

### Step 1: Create the Entity

```csharp
// ForgeKit.Api/Entities/Orders/Order.cs
using Api.Entities.Base;

namespace Api.Entities.Orders;

public class Order : BaseEntity
{
    public string CustomerId { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? FulfilledAt { get; set; }
    
    // Navigation properties
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public enum OrderStatus
{
    Pending = 0,
    Fulfilled = 1,
    Cancelled = 2
}

public class OrderItem : BaseEntity
{
    public string OrderId { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    
    // Navigation
    public Order Order { get; set; } = null!;
}
```

### Step 2: Add DbSet to AppDbContext

```csharp
// ForgeKit.Api/Data/AppDbContext.cs
public class AppDbContext : DbContext
{
    // Existing DbSets...
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
}
```

### Step 3: Configure Entity (Optional)

```csharp
// ForgeKit.Api/Data/Configurations/OrderConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.Property(o => o.TotalAmount)
            .HasPrecision(18, 2);  // Decimal precision

        builder.Property(o => o.CustomerId)
            .IsRequired()
            .HasMaxLength(50);

        // Relationships
        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.CreatedAt);

        // Query filter for soft delete (inherited from BaseEntity)
        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}
```

### Step 4: Apply Configuration in DbContext

```csharp
// ForgeKit.Api/Data/AppDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Apply all configurations in assembly
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```

### Step 5: Create Migration

```bash
cd api
dotnet ef migrations add AddOrderEntities --project ForgeKit.Api.Migrations.Sqlite --startup-project ForgeKit.Api.Migrations.Sqlite --context AppDbContext --output-dir Migrations/App
```

Generate an equivalent migration in the PostgreSQL and SQL Server migration projects before merging a shared model change. Apply only the migration project matching the configured runtime provider.

---

---

## Adding Custom Health Checks

Health checks monitor the operational status of your API and its dependencies. Add custom checks for external services, resources, or business logic.

### Creating a Custom Health Check

**1. Create Health Check Class:**

```csharp
// ForgeKit.Api/HealthChecks/EmailServiceHealthCheck.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.HealthChecks;

public class EmailServiceHealthCheck : IHealthCheck
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailServiceHealthCheck> _logger;

    public EmailServiceHealthCheck(
        IEmailService emailService,
        ILogger<EmailServiceHealthCheck> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform health check logic
            var isHealthy = await _emailService.TestConnectionAsync(cancellationToken);
            
            if (isHealthy)
            {
                return HealthCheckResult.Healthy(
                    "Email service is reachable",
                    data: new Dictionary<string, object>
                    {
                        ["server"] = _emailService.SmtpHost,
                        ["checkedAt"] = DateTime.UtcNow
                    });
            }
            
            return HealthCheckResult.Degraded("Email service connection is slow");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email service health check failed");
            return HealthCheckResult.Unhealthy(
                "Email service is unreachable",
                exception: ex);
        }
    }
}
```

**2. Register in Program.cs:**

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(tags: new[] { "ready", "database" })
    .AddCheck<EmailServiceHealthCheck>(
        "email-service",
        tags: new[] { "ready", "external" })
    .AddCheck("disk-space", () =>
    {
        var drive = new DriveInfo("C:\\");
        var freeSpaceGB = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
        
        return freeSpaceGB > 10
            ? HealthCheckResult.Healthy($"{freeSpaceGB}GB free")
            : HealthCheckResult.Unhealthy($"Only {freeSpaceGB}GB free");
    }, tags: new[] { "ready", "infrastructure" });
```

### Health Check Tags

Use tags to group health checks for different probes:

| Tag | Purpose | Endpoint |
|-----|---------|----------|
| `ready` | Readiness checks (DB, external APIs) | `/health/ready` |
| `live` | Liveness checks (basic process health) | `/health/live` |
| `database` | Database connectivity | `/health`, `/health/ready` |
| `external` | External service dependencies | `/health/ready` |
| `infrastructure` | Infrastructure (disk, memory) | `/health` |

**Example Response with Custom Check:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "appdbcontext": {
      "status": "Healthy",
      "duration": "00:00:00.0371813",
      "tags": ["ready", "database"]
    },
    "email-service": {
      "status": "Healthy",
      "duration": "00:00:00.0856234",
      "description": "Email service is reachable",
      "tags": ["ready", "external"],
      "data": {
        "server": "smtp.gmail.com",
        "checkedAt": "2026-02-11T07:45:00Z"
      }
    },
    "disk-space": {
      "status": "Healthy",
      "duration": "00:00:00.0012345",
      "description": "45GB free",
      "tags": ["ready", "infrastructure"]
    }
  }
}
```

### Common Health Check Patterns

**HTTP Endpoint Check:**
```csharp
builder.Services.AddHealthChecks()
    .AddUrlGroup(
        new Uri("https://api.example.com/health"),
        "external-api",
        tags: new[] { "ready", "external" });
```

**Redis Check:**
```csharp
builder.Services.AddHealthChecks()
    .AddRedis(
        "localhost:6379",
        "redis-cache",
        tags: new[] { "ready", "cache" });
```

**Database Check:**
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(
        name: "appdbcontext",
        tags: new[] { "ready", "database" });
```

### Testing Health Checks

**Integration Test:**
```csharp
[Fact]
public async Task EmailServiceHealthCheck_WhenServiceAvailable_ReturnsHealthy()
{
    // Arrange
    var emailServiceMock = new Mock<IEmailService>();
    emailServiceMock
        .Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);
    
    var healthCheck = new EmailServiceHealthCheck(
        emailServiceMock.Object,
        Mock.Of<ILogger<EmailServiceHealthCheck>>());
    
    // Act
    var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());
    
    // Assert
    result.Status.ShouldBe(HealthStatus.Healthy);
    result.Description.ShouldBe("Email service is reachable");
}
```

---

## Customizing Error Handling

### Creating Custom Exceptions

```csharp
// ForgeKit.Api/Exceptions/OrderException.cs
using Api.Exceptions;

namespace Api.Exceptions.Orders;

public class OrderNotFoundException : NotFoundException
{
    public OrderNotFoundException(string orderId)
        : base($"Order '{orderId}' not found")
    {
    }
}

public class InsufficientStockException : BusinessLogicException
{
    public InsufficientStockException(string productId, int requested, int available)
        : base($"Insufficient stock for product '{productId}'. Requested: {requested}, Available: {available}")
    {
    }
}
```

### Using Custom Exceptions

```csharp
if (order == null)
    throw new OrderNotFoundException(orderId);

if (product.Stock < quantity)
    throw new InsufficientStockException(product.Id, quantity, product.Stock);
```

**Note:** Custom exceptions are automatically caught by `ExceptionHandlingMiddleware` and converted to appropriate HTTP responses.

---

## Extending Middleware

### Creating Custom Middleware

```csharp
// ForgeKit.Api/Middlewares/RequestTimingMiddleware.cs
namespace Api.Middlewares;

public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(
        RequestDelegate next,
        ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            _logger.LogInformation(
                "Request {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                context.Response.StatusCode);
        }
    }
}
```

### Registering Middleware

```csharp
// ForgeKit.Api/Program.cs
app.UseMiddleware<RequestTimingMiddleware>();  // Add your middleware
```

**Important:** Middleware order matters! Place your middleware carefully in the pipeline.

---

## Testing Your Extensions

### Testing Handlers

```csharp
// ForgeKit.Api.Tests/Handlers/CreateOrderCommandHandlerTests.cs
public class CreateOrderCommandHandlerTests
{
    private readonly AppDbContext _context;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new AppDbContext(options);
        _logger = Substitute.For<ILogger<CreateOrderCommandHandler>>();
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_context, Substitute.For<IAuditContextService>(), _logger);
        var handler = new CreateOrderCommandHandler(unitOfWork, _logger);
        
        var command = new CreateOrderCommand(
            CustomerId: "customer123",
            Items: new List<OrderItemDto>
            {
                new("product1", 2, 10.00m)
            },
            TotalAmount: 20.00m);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<Result<OrderDto>.Success>();
        var success = (Result<OrderDto>.Success)result;
        success.Data.CustomerId.ShouldBe("customer123");
        success.Data.TotalAmount.ShouldBe(20.00m);
    }

    [Fact]
    public async Task HandleAsync_EmptyItems_ReturnsFailure()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_context, Substitute.For<IAuditContextService>(), _logger);
        var handler = new CreateOrderCommandHandler(unitOfWork, _logger);
        
        var command = new CreateOrderCommand(
            CustomerId: "customer123",
            Items: new List<OrderItemDto>(),
            TotalAmount: 0);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<Result<OrderDto>.Failure>();
        var failure = (Result<OrderDto>.Failure)result;
        failure.Code.ShouldBe("EMPTY_ORDER");
    }
}
```

**Note:** This project uses **Shouldly** for assertions.
```

### Testing Validators

```csharp
// ForgeKit.Api.Tests/Validators/CreateOrderCommandValidatorTests.cs
public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator;

    public CreateOrderCommandValidatorTests()
    {
        _validator = new CreateOrderCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: "customer123456789012345678",
            Items: new List<OrderItemDto> { new("product1", 1, 10.00m) },
            TotalAmount: 10.00m);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EmptyCustomerId_ShouldHaveError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: "",
            Items: new List<OrderItemDto> { new("product1", 1, 10.00m) },
            TotalAmount: 10.00m);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "CustomerId");
    }
}
```
```

---

## Best Practices

### ✅ DO

1. **Follow Naming Conventions**
   - Use consistent naming for commands, queries, handlers
   - Keep module names singular (e.g., `OrderModule`, not `OrdersModule`)

2. **Use Result Pattern**
   - Return `Result<T>` from handlers, not exceptions for business errors
   - Reserve exceptions for unexpected/unrecoverable errors

3. **Keep Handlers Thin**
   - Handlers should orchestrate, not implement business logic
   - Move complex logic to domain services

4. **Validate Early**
   - Use FluentValidation for input validation
   - Validate business rules in domain services

5. **Use Logging**
   - Log at entry/exit of handlers
   - Include correlation IDs for traceability

6. **Test Your Code**
   - Write unit tests for handlers and validators
   - Use integration tests for end-to-end scenarios

7. **Document Endpoints**
   - Use `.WithSummary()` and `.WithDescription()`
   - Add `.Produces<T>()` for all response types

### ❌ DON'T

1. **Don't Mix Concerns**
   - Keep commands and queries separate
   - Don't put business logic in endpoints

2. **Don't Use Magic Strings**
   - Define error codes as constants
   - Use enums for status values

3. **Don't Bypass Validation**
   - Don't skip validators for "simple" commands
   - Validation happens automatically; don't duplicate in handlers

4. **Don't Ignore Soft Delete**
   - Always inherit from `BaseEntity`
   - Let query filters handle soft-deleted records

5. **Don't Commit Sensitive Data**
   - Use User Secrets or environment variables
   - Never commit connection strings or API keys

6. **Don't Skip Migrations**
   - Always create migrations for entity changes
   - Test migrations on a separate database first

---

## Quick Checklist

Before committing your extension, ensure:

- [ ] Module follows naming conventions
- [ ] Commands/Queries use Result Pattern
- [ ] Validators created for all commands
- [ ] Entities inherit from BaseEntity
- [ ] Endpoints documented (summary, produces)
- [ ] Logging added to handlers
- [ ] Unit tests written and passing
- [ ] Migration created and tested
- [ ] No sensitive data committed
- [ ] Code reviewed for consistency

---

## Additional Resources

- [USER_GUIDE.md](./USER_GUIDE.md) - Architecture overview
- [CONFIGURATION_GUIDE.md](./CONFIGURATION_GUIDE.md) - Configuration reference
- [FLUENT_VALIDATION_GUIDE.md](./FLUENT_VALIDATION_GUIDE.md) - Validation patterns
- [RESULT_PATTERN_GUIDE.md](./RESULT_PATTERN_GUIDE.md) - Result pattern deep dive
- [docs/adr/](./adr/) - Architecture Decision Records
- [ForgeKit.Api/Samples/](../ForgeKit.Api/Samples/) - Reference implementations

---

**Version:** 1.0  
**Last Updated:** February 2026  
**Maintained by:** Development Team
