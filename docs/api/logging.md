# Structured Logging Examples and Best Practices

This guide demonstrates how to implement structured logging throughout the application, with examples for handlers, services, and middleware.

## Table of Contents
1. [Handler Logging](#handler-logging)
2. [Service Logging](#service-logging)
3. [Middleware Logging](#middleware-logging)
4. [Structured Properties](#structured-properties)
5. [Error Logging](#error-logging)
6. [Log Output Examples](#log-output-examples)
7. [Testing Logging](#testing-logging)

## Handler Logging

### Command Handler (Business-Significant Operation)

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using Api.Results;

public class CreateTodoHandler : ResultCommandHandler<CreateTodoCommand, TodoItemDto>
{
    private readonly ITodoService _service;
    private readonly ILogger<CreateTodoHandler> _logger;

    public CreateTodoHandler(
        ITodoService service,
        ILogger<CreateTodoHandler> logger) 
        : base(logger)
    {
        _service = service;
        _logger = logger;
    }

    public override async Task<Result<TodoItemDto>> HandleAsync(
        CreateTodoCommand request, 
        CancellationToken cancellationToken)
    {
        // Base class logs: "Executing CreateTodoHandler for CreateTodoCommand"
        
        try
        {
            var todoItem = await _service.CreateTodoAsync(
                request.WorkspaceId,
                request.Title,
                request.DueDate,
                cancellationToken);

            // Base class logs: "CreateTodoHandler completed successfully for CreateTodoCommand"
            return Success(todoItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating todo");
            // Base class logs: warning with failure details
            throw;
        }
    }
}
```

**Logging Output:**
```
[09:15:23 DBG] Executing CreateTodoHandler for CreateTodoCommand {X-Correlation-ID=abc123xyz}
[09:15:24 INF] Todo created: {TodoItemId=t-001, Status=Todo, CreatedBy=user-42}
[09:15:24 DBG] CreateTodoHandler completed successfully for CreateTodoCommand {X-Correlation-ID=abc123xyz}
```

### Query Handler (Informational)

```csharp
public class GetTodoByIdHandler : ResultQueryHandler<GetTodoByIdQuery, TodoItemDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetTodoByIdHandler> _logger;

    public GetTodoByIdHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetTodoByIdHandler> logger)
        : base(logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public override async Task<Result<TodoItemDto>> HandleAsync(
        GetTodoByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Base class logs entry at Debug level
        
        var todoItem = await _unitOfWork.DbContext.TodoItems
            .FirstOrDefaultAsync(t => t.Id == request.TodoItemId, cancellationToken);

        if (todoItem == null)
        {
            _logger.LogDebug("Todo not found: {TodoItemId}", request.TodoItemId);
            return NotFound("Todo not found", "TodoItem");
        }

        var dto = new TodoItemDto { /* ... */ };
        
        // Base class logs success at Debug level (queries don't have business significance)
        return Success(dto);
    }
}
```

**Logging Output:**
```
[10:22:45 DBG] Executing GetTodoByIdHandler for GetTodoByIdQuery {X-Correlation-ID=def456uvw}
[10:22:45 DBG] Todo not found: {TodoItemId=t-404}
[10:22:45 DBG] GetTodoByIdHandler completed successfully for GetTodoByIdQuery {X-Correlation-ID=def456uvw}
```

## Service Logging

### Domain Service with Comprehensive Logging

```csharp
using Microsoft.Extensions.Logging;

public class TodoService : ITodoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditContext _auditContext;
    private readonly ISoftDeleteDomainService _softDeleteService;
    private readonly ILogger<TodoService> _logger;

    public TodoService(
        IUnitOfWork unitOfWork,
        IAuditContext auditContext,
        ISoftDeleteDomainService softDeleteService,
        ILogger<TodoService> logger)
    {
        _unitOfWork = unitOfWork;
        _auditContext = auditContext;
        _softDeleteService = softDeleteService;
        _logger = logger;
    }

    public async Task<TodoItemDto> CreateTodoAsync(
        string workspaceId,
        string title,
        DateTime? dueDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating todo {Title} due {DueDate}", title, dueDate);

        // Validation
        if (dueDate < DateTime.UtcNow.Date)
        {
            _logger.LogWarning(
                "Todo with past due date rejected: {DueDate} (today={Today})",
                dueDate, DateTime.UtcNow.Date);
            throw new ValidationException("Due date cannot be in the past");
        }

        try
        {
            _unitOfWork.BeginTransaction();

            var todoItem = new TodoItem
            {
                Id = Guid.NewGuid().ToString("N"),
                WorkspaceId = workspaceId,
                Title = title,
                DueDate = dueDate,
                CurrentStatus = "Todo",
                // Audit fields set by DbContext interceptor
            };

            _unitOfWork.DbContext.TodoItems.Add(todoItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _unitOfWork.CommitTransaction();

            _logger.LogInformation(
                "Todo created successfully: {TodoItemId} by {UserId} due {DueDate}",
                todoItem.Id,
                _auditContext.UserId,
                dueDate);

            return new TodoItemDto
            {
                Id = todoItem.Id,
                Status = todoItem.CurrentStatus,
                CreatedBy = _auditContext.UserId,
                // ...
            };
        }
        catch (Exception ex)
        {
            _unitOfWork.RollbackTransaction();
            _logger.LogError(
                ex,
                "Failed to create todo {Title}: {ErrorMessage}",
                title,
                ex.Message);
            throw;
        }
    }

    public async Task UpdateTodoStatusAsync(
        string todoItemId,
        string status,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating todo status: {TodoItemId} -> {Status}", todoItemId, status);

        var todoItem = await _unitOfWork.DbContext.TodoItems.FirstOrDefaultAsync(
            t => t.Id == todoItemId, cancellationToken);

        if (todoItem == null)
        {
            _logger.LogWarning("Status update attempted on non-existent todo: {TodoItemId}", todoItemId);
            throw new NotFoundException($"Todo {todoItemId} not found");
        }

        if (todoItem.CurrentStatus == "Cancelled")
        {
            _logger.LogWarning(
                "Status update rejected: invalid transition from {CurrentStatus} to {NewStatus} for {TodoItemId}",
                todoItem.CurrentStatus,
                status,
                todoItemId);
            throw new ValidationException(
                $"Cannot update a todo in {todoItem.CurrentStatus} status");
        }

        var oldStatus = todoItem.CurrentStatus;
        todoItem.CurrentStatus = status;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Todo status updated: {TodoItemId} status {OldStatus}->{NewStatus} by {UserId}",
            todoItemId,
            oldStatus,
            status,
            _auditContext.UserId);
    }
}
```

**Logging Output for CreateTodoAsync (Success):**
```
[14:32:10 DBG] Creating todo Review the launch plan due 2026-02-15
[14:32:11 INF] Todo created successfully: t-xyz123 by user-42 due 2026-02-15
```

**Logging Output for CreateTodoAsync (Validation Failure):**
```
[14:33:20 DBG] Creating todo Review the launch plan due 2026-02-01
[14:33:20 WRN] Todo with past due date rejected: 2026-02-01 (today=2026-02-09)
```

**Logging Output for UpdateTodoStatusAsync (Status Transition):**
```
[14:35:45 DBG] Updating todo status: t-xyz123 -> Done
[14:35:46 INF] Todo status updated: t-xyz123 status InProgress->Done by user-42
```

## Middleware Logging

### Correlation ID Middleware

The middleware automatically injects correlation ID into all logs:

```csharp
public class CorrelationIdMiddleware : IMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = context.Request.Headers
            .TryGetValue(CorrelationIdHeader, out var headerValue) 
            ? headerValue.ToString().Trim() 
            : null;

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        // Push correlation ID into Serilog LogContext
        using (LogContext.PushProperty("X-Correlation-ID", correlationId))
        {
            context.Response.Headers.Add(CorrelationIdHeader, correlationId);
            await next(context);
        }
    }
}
```

**Result:** Every log from this request onwards includes `{X-Correlation-ID=abc123xyz}`

## Structured Properties

### Best Practices for Adding Properties

✅ **Good - Specific, named properties:**
```csharp
_logger.LogInformation(
    "Order processed: {OrderId} for customer {CustomerId} with total {Amount} in {Duration}ms",
    orderId,
    customerId,
    totalAmount,
    stopwatch.ElapsedMilliseconds);
```

❌ **Bad - Unstructured string concatenation:**
```csharp
_logger.LogInformation($"Order {orderId} processed in {elapsed}ms");
```

✅ **Good - Complex objects as structured properties:**
```csharp
_logger.LogInformation(
    "User created: {User}",
    new { UserId = user.Id, Email = user.Email, Role = user.Role });
```

## Error Logging

### Exception Logging in Handlers

```csharp
try
{
    // Business logic
}
catch (ValidationException ex)
{
    // Validation errors are business logic, not system errors
    _logger.LogWarning(
        "Validation failed: {Code} - {Message}",
        "INPUT_INVALID",
        ex.Message);
    return Failure(ex.Message);
}
catch (NotFoundException ex)
{
    // Not found is expected for some queries
    _logger.LogDebug("Resource not found: {Message}", ex.Message);
    return NotFound(ex.Message);
}
catch (Exception ex)
{
    // Unexpected errors should be logged with full context
    _logger.LogError(
        ex,
        "Unexpected error processing {Operation} with {Context}",
        operationType,
        contextInfo);
    throw;
}
```

### Middleware Exception Logging

ExceptionHandlingMiddleware automatically logs exceptions with:
- Full exception stack trace (Error level)
- HTTP status code and response
- Correlation ID for tracing
- No sensitive data (tokens, passwords)

```
[15:47:22 ERR] System.InvalidOperationException: Database connection failed
      at Api.Data.UnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
      at Api.Services.TodoService.CreateTodoAsync(String workspaceId, String title, DateTime? dueDate, CancellationToken cancellationToken)
      ...
[15:47:22 INF] Error response: HTTP 500, Status=InternalServerError, TraceId=t-xyz123:{X-Correlation-ID=abc123xyz}
```

## Log Output Examples

### Console Output (Development)
```
[09:15:23 DBG] Executing CreateTodoHandler for CreateTodoCommand {X-Correlation-ID=abc123xyz}
[09:15:23 DBG] Creating todo Review the launch plan due 2026-02-15
[09:15:24 INF] Todo created successfully: t-xyz123 by user-42 due 2026-02-15
[09:15:24 INF] TodoService completed successfully {X-Correlation-ID=abc123xyz}
[09:15:24 DBG] CreateTodoHandler completed successfully for CreateTodoCommand {X-Correlation-ID=abc123xyz}
```

### File Output (logs/log-YYYYMMDD.txt)
Same format as console, organized daily.

### Log Aggregation Service (Production)
Structure easily parsed as JSON or key=value pairs:
```json
{
  "Timestamp": "2026-02-09T15:47:22.1234567Z",
  "Level": "Information",
  "MessageTemplate": "Todo created successfully: {TodoItemId} by {UserId} due {DueDate}",
  "Properties": {
    "TodoItemId": "t-xyz123",
    "UserId": "user-42",
    "DueDate": "2026-02-15",
    "X-Correlation-ID": "abc123xyz"
  }
}
```

## Testing Logging

### Unit Testing with Mock Logger

```csharp
[Fact]
public async Task CreateTodoHandler_LogsSuccessfully()
{
    // Arrange
    var mockLogger = Substitute.For<ILogger<CreateTodoHandler>>();
    var mockService = Substitute.For<ITodoService>();
    var handler = new CreateTodoHandler(mockService, mockLogger);
    var command = new CreateTodoCommand { Title = "Review the launch plan", DueDate = DateTime.UtcNow.AddDays(1) };

    mockService.CreateTodoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime?>())
        .Returns(new TodoItemDto { Id = "t-001" });

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.ShouldBeOfType<Result<TodoItemDto>.Success>();
    
    // Verify logging occurred
    mockLogger.Received().Log(
        Arg.Any<LogLevel>(),
        Arg.Any<EventId>(),
        Arg.Any<IReadOnlyList<KeyValuePair<string, object>>>(),
        Arg.Any<Exception?>(),
        Arg.Any<Func<IReadOnlyList<KeyValuePair<string, object>>, Exception?, string>>());
}
```

**Note:** This project uses **Shouldly** for assertions.

### Integration Testing with Real Logs

```csharp
[Fact]
public async Task CreateTodoItem_EndToEnd_CreatesLogWithCorrelationId()
{
    // Arrange
    var client = _factory.CreateClient();
    var correlationId = "test-correlation-123";
    
    // Act
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/todos");
    request.Headers.Add("X-Correlation-ID", correlationId);
    request.Content = new StringContent(
        JsonSerializer.Serialize(new { title = "Review the launch plan", dueDate = DateTime.UtcNow.AddDays(1) }),
        Encoding.UTF8,
        "application/json");
    
    var response = await client.SendAsync(request);

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    response.Headers.ShouldContainKey("X-Correlation-ID");
    
    // Verify correlation ID in logs (would be in ForgeKit.Api/logs/log-*.txt)
    // In real testing, parse log file and validate presence
}
```

**Note:** This project uses **Shouldly** for assertions.

## Summary

- **Command Handlers**: Log at Information level on success (business-significant)
- **Query Handlers**: Log at Debug level on success (informational only)
- **Services**: Log Debug for validation, Information for operations, Warning for conflicts
- **Correlation ID**: Automatically injected and available in all logs
- **Testing**: Mock loggers for unit tests, real logs for integration tests
- **Safety**: Never log passwords, tokens, or sensitive PII
