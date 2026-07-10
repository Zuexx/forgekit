# 如何使用 FluentValidation for DTO 验证

本文档说明目前的实现中，如何为 DTO 应用 FluentValidation 验证。

## 整体架构

```
Request/Command/Query (DTO)
    ↓
ValidationBehavior (MediatR Pipeline)
    ↓ (validators injected via DI)
IValidator<TRequest> implementations
    ↓ (fail-fast)
ValidationAppException (if validation fails)
    ↓
ExceptionHandlingMiddleware (converts to HTTP 400)
```

---

## 1. 当前实现的注册方式

### Program.cs 中的配置

```csharp
// Line 82-87 in ForgeKit.Api/Program.cs

// Register FluentValidation service - auto-discovers all validators
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Register MediatR pipeline behaviors (validation)
builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

**关键点**：
- `AddValidatorsFromAssembly()` 自动发现所有实现 `IValidator<T>` 的类
- `ValidationBehavior<,>` 被注册为 MediatR 管道行为
- 任何 MediatR request 都会自动通过 ValidationBehavior 验证

---

## 2. 创建 DTO 和验证器

### 步骤 1: 定义 Request DTO

```csharp
// ForgeKit.Api/Samples/SampleCreateResourceCommand.cs
using MediatR;
using Api.Results;

namespace Api.Samples;

/// <summary>
/// Command to create a new resource.
/// </summary>
public record CreateResourceCommand(
    string Name,
    string? Description,
    int Quantity
) : IRequest<Result<CreatedResourceDto>>;
```

### 步骤 2: 创建 FluentValidation 验证器

```csharp
// ForgeKit.Api/Samples/Validators/CreateResourceCommandValidator.cs
using FluentValidation;

namespace Api.Samples.Validators;

public class CreateResourceCommandValidator : AbstractValidator<CreateResourceCommand>
{
    public CreateResourceCommandValidator()
    {
        // Name validation
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MinimumLength(3)
            .WithMessage("Name must be at least 3 characters")
            .MaximumLength(100)
            .WithMessage("Name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_]+$")
            .WithMessage("Name can only contain alphanumeric characters, spaces, hyphens, and underscores");

        // Description validation (optional but if provided, must meet criteria)
        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        // Quantity validation
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(10000)
            .WithMessage("Quantity cannot exceed 10000");
    }
}
```

---

## 3. 验证流程详解

### 自动验证流程

当您调用 handler 时：

```csharp
// 在 endpoint 或客户端代码中
var command = new CreateResourceCommand("Test", "A test resource", 5);

// 当通过 mediator 发送请求时...
var result = await mediator.Send(command, cancellationToken);

// ...发生的事情是：
// 1. MediatR 管道开始
// 2. ValidationBehavior 被调用
// 3. 自动查找所有 IValidator<CreateResourceCommand> 的实现
// 4. 调用 CreateResourceCommandValidator.Validate()
// 5. 如果失败，抛出 ValidationAppException
// 6. 如果成功，调用实际的 handler
```

### ValidationBehavior 的工作原理

```csharp
// ForgeKit.Api/Behaviors/ValidationBehavior.cs

public async Task<TResponse> Handle(
    TRequest request, 
    RequestHandlerDelegate<TResponse> next, 
    CancellationToken cancellationToken)
{
    if (!_validators.Any())
    {
        return await next();  // 没有验证器，直接调用 handler
    }

    var context = new ValidationContext<TRequest>(request);

    // 执行所有验证器
    var errorsDictionary = _validators
        .Select(x => x.Validate(context))
        .SelectMany(x => x.Errors)
        .Where(x => x != null)
        .GroupBy(
            x => x.PropertyName,
            x => x.ErrorMessage,
            (propertyName, errorMessages) => new
            {
                Key = propertyName,
                Values = errorMessages.Distinct().ToArray()
            })
        .ToDictionary(x => x.Key, x => x.Values);

    // 如果有错误，抛出异常
    if (errorsDictionary.Count > 0)
    {
        throw new ValidationAppException(errorsDictionary);
    }

    // 验证成功，调用 handler
    return await next(cancellationToken);
}
```

---

## 4. 错误响应格式

当验证失败时，返回的错误格式如下：

### HTTP Response

```
POST /v1/resources
{
  "name": "",
  "quantity": -5
}

HTTP 400 Bad Request

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "errors": {
    "name": [
      "Name is required",
      "Name must be at least 3 characters"
    ],
    "quantity": [
      "Quantity must be greater than 0"
    ]
  }
}
```

---

## 5. 完整的实现示例

### 示例: 创建 Visit Request

```csharp
// ForgeKit.Api/Modules/Todos/Commands/CreateTodoCommand.cs
using MediatR;
using Api.Results;

namespace Api.Entities.Visits.Requests;

public record CreateTodoCommand(
    string AssignedToMemberId,
    string WorkspaceId,
    DateTime ScheduledDate,
    string? Notes
) : IRequest<Result<TodoItemDto>>;

public record TodoItemDto(
    string Id,
    string AssignedToMemberId,
    DateTime ScheduledDate,
    DateTime CreatedAt
);
```

### 对应的验证器

```csharp
// ForgeKit.Api/Entities/Visits/Requests/Validators/CreateTodoCommandValidator.cs
using FluentValidation;

namespace Api.Entities.Visits.Requests.Validators;

public class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator()
    {
        // Healthcare Professional ID
        RuleFor(x => x.AssignedToMemberId)
            .NotEmpty()
            .WithMessage("Healthcare Professional ID is required")
            .Matches(@"^[a-f0-9\-]{36}$")
            .WithMessage("Healthcare Professional ID must be a valid UUID");

        // Medical Representative ID
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Medical Representative ID is required")
            .Matches(@"^[a-f0-9\-]{36}$")
            .WithMessage("Medical Representative ID must be a valid UUID");

        // Scheduled Date
        RuleFor(x => x.ScheduledDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Scheduled date must be in the future")
            .LessThanOrEqualTo(DateTime.UtcNow.AddYears(1))
            .WithMessage("Scheduled date cannot be more than 1 year in the future");

        // Notes (optional)
        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .WithMessage("Notes cannot exceed 2000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}
```

### Handler（验证自动进行）

```csharp
// ForgeKit.Api/Modules/Todos/Commands/CreateTodoCommandHandler.cs
using Api.Handlers;
using Api.Results;
using MediatR;

namespace Api.Entities.Visits.Requests;

public class CreateTodoCommandHandler(
    ILogger<CreateTodoCommandHandler> logger,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) 
    : ResultCommandHandler<CreateTodoCommand, TodoItemDto>(logger)
{
    public override async Task<Result<TodoItemDto>> HandleAsync(
        CreateTodoCommand request,
        CancellationToken cancellationToken)
    {
        // 不需要手动验证！ValidationBehavior 已经做了
        // 如果执行到这里，request 一定是有效的

        // 业务逻辑：检查医疗专业人员是否存在
        var professional = await unitOfWork.TodoItems.GetMemberAsync(
            request.AssignedToMemberId, 
            cancellationToken);

        if (professional == null)
        {
            return NotFound(
                $"Healthcare professional with ID '{request.AssignedToMemberId}' not found",
                "healthcareProfessionalId"
            );
        }

        // 创建新 visit request
        var todoItem = new TodoItem
        {
            Id = Guid.NewGuid().ToString(),
            AssignedToMemberId = request.AssignedToMemberId,
            WorkspaceId = request.WorkspaceId,
            ScheduledDate = request.ScheduledDate,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = auditContext.CurrentUserId
        };

        unitOfWork.TodoItems.Add(todoItem);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new TodoItemDto(
            todoItem.Id,
            todoItem.AssignedToMemberId,
            todoItem.ScheduledDate,
            todoItem.CreatedAt
        );

        return Success(dto);
    }
}
```

---

## 6. 高级验证场景

### 跨字段验证

```csharp
public class ComplexCommandValidator : AbstractValidator<ComplexCommand>
{
    public ComplexCommandValidator()
    {
        RuleFor(x => x)
            .Custom((request, context) =>
            {
                // 比较两个字段
                if (request.EndDate <= request.StartDate)
                {
                    context.AddFailure("endDate", "End date must be after start date");
                }

                // 条件性业务规则
                if (request.Type == "Premium" && request.Price < 100)
                {
                    context.AddFailure("price", "Premium resources must cost at least 100");
                }
            });
    }
}
```

### 异步验证（与数据库交互）

```csharp
public class UniqueEmailCommandValidator : AbstractValidator<RegisterUserCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public UniqueEmailCommandValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Email must be valid")
            .MustAsync(BeUniqueEmail)
            .WithMessage("Email address is already registered");
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        var existingUser = await _unitOfWork.Users.FindByEmailAsync(email, cancellationToken);
        return existingUser == null;
    }
}
```

### 集合验证

```csharp
public class BulkCreateCommandValidator : AbstractValidator<BulkCreateCommand>
{
    public BulkCreateCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Items list cannot be empty")
            .Must(items => items.Count <= 100)
            .WithMessage("Cannot create more than 100 items at once");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateItemValidator());
    }
}

public class CreateItemValidator : AbstractValidator<CreateItemRequest>
{
    public CreateItemValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Item name is required");
    }
}
```

---

## 7. 在 Module 中注册验证器

如果验证器在 Module 内组织，确保在 `RegisterModule` 中添加：

```csharp
// ForgeKit.Api/Modules/VisitsModule.cs
public class VisitsModule : IModule
{
    public IServiceCollection RegisterModule(IServiceCollection services)
    {
        // MediatR 会自动发现所有处理程序和验证器
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(typeof(VisitsModule).Assembly));

        // 显式注册该 module 的验证器（如果需要）
        services.AddValidatorsFromAssembly(typeof(VisitsModule).Assembly);

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // ... endpoint mapping
    }
}
```

---

## 8. 最佳实践

| 最佳实践 | 说明 |
|---------|------|
| **一个 Validator per Request** | 每个 Command/Query 创建对应的 Validator |
| **验证器与 Request 同位置** | 放在同一个文件或同一个文件夹 |
| **描述性的错误消息** | 帮助客户端理解如何修正 |
| **使用异步验证** | 对于需要数据库查询的验证使用 `MustAsync` |
| **避免在 Handler 中验证** | ValidationBehavior 已经做了，handler 专注业务逻辑 |
| **测试验证器** | 验证器应该有独立的单元测试 |

---

## 9. 测试验证器

```csharp
// ForgeKit.Api.Tests/Validators/CreateResourceCommandValidatorTests.cs
using Api.Samples;
using Api.Samples.Validators;
using FluentValidation.TestHelper;
using Shouldly;

namespace ForgeKit.Api.Tests.Validators;

public class CreateResourceCommandValidatorTests
{
    private readonly CreateResourceCommandValidator _validator;

    public CreateResourceCommandValidatorTests()
    {
        _validator = new CreateResourceCommandValidator();
    }

    [Fact]
    public void Validator_WithEmptyName_HasError()
    {
        var command = new CreateResourceCommand("", null, 5);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validator_WithValidData_IsValid()
    {
        var command = new CreateResourceCommand("Test Resource", "Description", 5);

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validator_WithShortName_HasError()
    {
        var command = new CreateResourceCommand("AB", null, 5);

        var result = _validator.TestValidate(command);

        var errors = result.ShouldHaveValidationErrorFor(x => x.Name);
        errors.ShouldContain(e => e.ErrorMessage == "Name must be at least 3 characters");
    }
}
```

**Note:** This project uses **Shouldly** for assertions (MIT license), not FluentAssertions.

---

## 总结

当前的 ForgeKit 实现中：

✅ **自动化**：验证器自动被发现和注入  
✅ **声明式**：使用 FluentValidation 的流畅 API  
✅ **可重用**：验证器可跨多个处理程序使用  
✅ **集中化**：所有验证在 ValidationBehavior 中统一处理  
✅ **标准化**：所有验证错误返回统一的 HTTP 400 格式  
✅ **可测试**：验证器可独立测试  

只需：
1. 创建 Request record
2. 创建继承 `AbstractValidator<T>` 的验证器
3. 定义验证规则
4. Handler 自动获得验证保障！
