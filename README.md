# Mediator

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-106%20passed-brightgreen.svg)]()
[![Coverage](https://img.shields.io/badge/coverage-100%25-brightgreen.svg)]()

A lightweight, zero-configuration mediator implementation for .NET supporting the CQRS pattern with request/response handling, notification publishing, and extensible pipeline behaviors.

## Features

- **Request/Response Pattern** - Type-safe command and query handling with `IRequest<TResponse>`
- **Publish/Subscribe Pattern** - Notification broadcasting to multiple handlers via `INotification`
- **Pipeline Behaviors** - Middleware-style cross-cutting concerns for both requests and notifications
- **Automatic Registration** - Assembly scanning with zero configuration
- **100% Test Coverage** - 106 passing tests covering all scenarios
- **.NET Standard 2.0** - Compatible with .NET Framework 4.6.1+ and .NET Core 2.0+
- **Single Dependency** - Only requires `Microsoft.Extensions.DependencyInjection.Abstractions`
- **High Performance** - Efficient service resolution with minimal overhead

## Installation

```bash
dotnet add package *PENDING*
```

## Table of Contents

- [Quick Start](#quick-start)
- [Request/Response Pattern](#requestresponse-pattern)
  - [Queries](#queries-read-operations)
  - [Commands with Response](#commands-with-response)
  - [Commands without Response](#commands-without-response)
- [Publish/Subscribe Pattern](#publishsubscribe-pattern)
  - [Creating Notifications](#creating-notifications)
  - [Multiple Handlers](#multiple-handlers)
  - [Exception Handling](#exception-handling-in-notifications)
- [Pipeline Behaviors](#pipeline-behaviors)
  - [Request Pipeline Behaviors](#request-pipeline-behaviors)
  - [Notification Pipeline Behaviors](#notification-pipeline-behaviors)
- [Advanced Usage](#advanced-usage)
- [Architecture](#architecture)
- [Testing](#testing)
- [API Reference](#api-reference)
- [License](#license)

## Quick Start

### 1. Register Services

Register the mediator and scan assemblies for handlers:

```csharp
using Mediator;

var builder = WebApplication.CreateBuilder(args);

// Scans the executing assembly for handlers and behaviors
builder.Services.AddMediator(Assembly.GetExecutingAssembly());
```

### 2. Define a Request and Handler

```csharp
// Request
public record GetUserQuery(int UserId) : IRequest<UserDto>;

// Response
public record UserDto(int Id, string Name, string Email);

// Handler
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    private readonly IUserRepository _repository;

    public GetUserQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserDto> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.UserId, cancellationToken);
        return new UserDto(user.Id, user.Name, user.Email);
    }
}
```

### 3. Send Requests

```csharp
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id, CancellationToken cancellationToken)
    {
        var user = await _mediator.SendAsync(new GetUserQuery(id), cancellationToken);
        return Ok(user);
    }
}
```

## Request/Response Pattern

The request/response pattern implements the mediator pattern for one-to-one communication between a request and its handler.

### Queries (Read Operations)

Queries fetch data without modifying state:

```csharp
// Define query and response
public record GetProductsQuery(int CategoryId) : IRequest<IEnumerable<ProductDto>>;

public record ProductDto(int Id, string Name, decimal Price);

// Implement handler
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>
{
    private readonly IProductRepository _repository;

    public GetProductsQueryHandler(IProductRepository repository) => _repository = repository;

    public async Task<IEnumerable<ProductDto>> HandleAsync(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var products = await _repository.GetByCategoryAsync(request.CategoryId, cancellationToken);
        return products.Select(p => new ProductDto(p.Id, p.Name, p.Price));
    }
}

// Usage
var products = await _mediator.SendAsync(new GetProductsQuery(categoryId), cancellationToken);
```

### Commands with Response

Commands that modify state and return data:

```csharp
public record CreateOrderCommand(int UserId, IEnumerable<OrderItem> Items) : IRequest<int>;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, int>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(IOrderRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> HandleAsync(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order(request.UserId, request.Items);
        await _repository.AddAsync(order, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return order.Id;
    }
}

// Usage
var orderId = await _mediator.SendAsync(new CreateOrderCommand(userId, items), cancellationToken);
```

### Commands without Response

Commands that perform actions without returning data use the `Unit` type:

```csharp
public record DeleteProductCommand(int ProductId) : IRequest;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Unit>
{
    private readonly IProductRepository _repository;

    public DeleteProductCommandHandler(IProductRepository repository) => _repository = repository;

    public async Task<Unit> HandleAsync(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        await _repository.DeleteAsync(request.ProductId, cancellationToken);
        return Unit.Value;
    }
}

// Usage (response can be ignored)
await _mediator.SendAsync(new DeleteProductCommand(productId), cancellationToken);
```

## Publish/Subscribe Pattern

The publish/subscribe pattern enables one-to-many communication where a single notification can be handled by multiple handlers concurrently.

### Creating Notifications

Define notifications by implementing `INotification`:

```csharp
public record OrderPlacedNotification(int OrderId, int UserId, decimal TotalAmount) : INotification;

// Multiple handlers can process this notification
public class SendOrderConfirmationEmailHandler : INotificationHandler<OrderPlacedNotification>
{
    private readonly IEmailService _emailService;

    public SendOrderConfirmationEmailHandler(IEmailService emailService) => _emailService = emailService;

    public async Task HandleAsync(OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        await _emailService.SendOrderConfirmationAsync(
            notification.UserId,
            notification.OrderId,
            cancellationToken);
    }
}

public class UpdateInventoryHandler : INotificationHandler<OrderPlacedNotification>
{
    private readonly IInventoryService _inventoryService;

    public UpdateInventoryHandler(IInventoryService inventoryService) => _inventoryService = inventoryService;

    public async Task HandleAsync(OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        await _inventoryService.ReserveInventoryAsync(notification.OrderId, cancellationToken);
    }
}

public class CreateShipmentHandler : INotificationHandler<OrderPlacedNotification>
{
    private readonly IShipmentService _shipmentService;

    public CreateShipmentHandler(IShipmentService shipmentService) => _shipmentService = shipmentService;

    public async Task HandleAsync(OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        await _shipmentService.CreateShipmentAsync(notification.OrderId, cancellationToken);
    }
}
```

### Publishing Notifications

```csharp
// All registered handlers execute in parallel
await _mediator.Publish(
    new OrderPlacedNotification(orderId, userId, totalAmount),
    cancellationToken);
```

### Multiple Handlers

All handlers for a notification execute **concurrently** using `Task.WhenAll`. This maximizes throughput for independent operations.

```csharp
// These three handlers run in parallel:
// - SendOrderConfirmationEmailHandler
// - UpdateInventoryHandler
// - CreateShipmentHandler
await _mediator.Publish(notification, cancellationToken);
```

### Exception Handling in Notifications

**Single Handler Exception**: If only one handler throws an exception, that exception is rethrown directly.

**Multiple Handler Exceptions**: If multiple handlers throw exceptions, an `AggregateException` containing all exceptions is thrown.

```csharp
try
{
    await _mediator.Publish(notification, cancellationToken);
}
catch (AggregateException ex) when (ex.InnerExceptions.Count > 1)
{
    // Multiple handlers failed
    foreach (var innerException in ex.InnerExceptions)
    {
        _logger.LogError(innerException, "Handler failed");
    }
}
catch (Exception ex)
{
    // Single handler failed
    _logger.LogError(ex, "Handler failed");
}
```

## Pipeline Behaviors

Pipeline behaviors provide a middleware-style mechanism for cross-cutting concerns. Behaviors execute in reverse registration order (last registered executes first).

### Request Pipeline Behaviors

Request pipeline behaviors wrap request handlers, enabling pre/post-processing logic:

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandler<TResponse> nextHandler,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}: {@Request}", requestName, request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await nextHandler();
            stopwatch.Stop();

            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Error handling {RequestName} after {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

#### Validation Behavior

```csharp
public interface IValidatable
{
    IEnumerable<string> Validate();
}

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandler<TResponse> nextHandler,
        CancellationToken cancellationToken)
    {
        if (request is IValidatable validatable)
        {
            var errors = validatable.Validate().ToList();
            if (errors.Any())
            {
                throw new ValidationException(string.Join(", ", errors));
            }
        }

        return await nextHandler();
    }
}

// Apply validation to requests
public record CreateUserCommand(string Name, string Email) : IRequest<int>, IValidatable
{
    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return "Name is required";

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
            yield return "Valid email is required";
    }
}
```

#### Caching Behavior with Short-Circuiting

Behaviors can short-circuit the pipeline by not calling `nextHandler()`:

```csharp
public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan CacheDuration { get; }
}

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(IDistributedCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandler<TResponse> nextHandler,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheable cacheable)
            return await nextHandler();

        var cacheKey = cacheable.CacheKey;

        // Try cache
        var cachedValue = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cachedValue != null)
        {
            _logger.LogInformation("Cache hit for {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<TResponse>(cachedValue)!;
        }

        // Execute handler
        _logger.LogInformation("Cache miss for {CacheKey}", cacheKey);
        var response = await nextHandler();

        // Store in cache
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(response),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = cacheable.CacheDuration },
            cancellationToken);

        return response;
    }
}

// Apply caching to queries
public record GetUserQuery(int UserId) : IRequest<UserDto>, ICacheable
{
    public string CacheKey => $"user:{UserId}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
}
```

#### Transaction Behavior

```csharp
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandler<TResponse> nextHandler,
        CancellationToken cancellationToken)
    {
        // Only wrap commands (requests that modify state)
        if (request is ICommand)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var response = await nextHandler();
                await _unitOfWork.CommitAsync(cancellationToken);
                return response;
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }

        return await nextHandler();
    }
}
```

### Notification Pipeline Behaviors

Notification pipeline behaviors wrap each notification handler independently:

```csharp
public class NotificationLoggingBehavior<TNotification> : INotificationPipelineBehavior<TNotification>
    where TNotification : INotification
{
    private readonly ILogger<NotificationLoggingBehavior<TNotification>> _logger;

    public NotificationLoggingBehavior(ILogger<NotificationLoggingBehavior<TNotification>> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(
        TNotification notification,
        NotificationHandler nextHandler,
        CancellationToken cancellationToken)
    {
        var notificationName = typeof(TNotification).Name;
        _logger.LogInformation("Processing {NotificationName}: {@Notification}", notificationName, notification);

        try
        {
            await nextHandler();
            _logger.LogInformation("Processed {NotificationName}", notificationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {NotificationName}", notificationName);
            throw;
        }
    }
}
```

#### Notification Retry Behavior

```csharp
public class NotificationRetryBehavior<TNotification> : INotificationPipelineBehavior<TNotification>
    where TNotification : INotification
{
    private readonly ILogger<NotificationRetryBehavior<TNotification>> _logger;
    private const int MaxRetries = 3;

    public NotificationRetryBehavior(ILogger<NotificationRetryBehavior<TNotification>> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(
        TNotification notification,
        NotificationHandler nextHandler,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (attempt < MaxRetries)
        {
            try
            {
                await nextHandler();
                return;
            }
            catch (Exception ex) when (attempt < MaxRetries - 1)
            {
                attempt++;
                _logger.LogWarning(
                    ex,
                    "Notification handler failed, attempt {Attempt}/{MaxRetries}",
                    attempt,
                    MaxRetries);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
            }
        }
    }
}
```

### Behavior Execution Order

Behaviors execute in **reverse registration order**. The last registered behavior executes first:

```csharp
services.AddMediator(Assembly.GetExecutingAssembly());

// Execution order:
// 1. TransactionBehavior (outermost)
// 2. ValidationBehavior
// 3. LoggingBehavior
// 4. Handler (innermost)
```

Pipeline flow:

```
Request → TransactionBehavior → ValidationBehavior → LoggingBehavior → Handler → Response
```

## Advanced Usage

### Multiple Assembly Registration

Scan multiple assemblies for handlers and behaviors:

```csharp
services.AddMediator(
    typeof(Program).Assembly,              // API layer
    typeof(CreateOrderCommand).Assembly,   // Application layer
    typeof(OrderRepository).Assembly       // Infrastructure layer
);
```

### Custom Service Lifetimes

By default, all handlers and behaviors are registered as **scoped**. To use different lifetimes, register them manually:

```csharp
// Register mediator
services.AddMediator(Assembly.GetExecutingAssembly());

// Override specific handler lifetime
services.AddSingleton<IRequestHandler<GetCachedDataQuery, CachedData>, GetCachedDataQueryHandler>();
```

### Conditional Behavior Execution

Behaviors can conditionally execute based on request attributes:

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class NoLoggingAttribute : Attribute { }

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandler<TResponse> nextHandler,
        CancellationToken cancellationToken)
    {
        // Skip logging if request has NoLogging attribute
        if (typeof(TRequest).GetCustomAttribute<NoLoggingAttribute>() != null)
            return await nextHandler();

        // ... logging logic
    }
}

// Apply attribute to skip logging
[NoLogging]
public record GetSensitiveDataQuery(int UserId) : IRequest<SensitiveData>;
```

### Handler Dependencies

Handlers support full dependency injection:

```csharp
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, int>
{
    private readonly IOrderRepository _repository;
    private readonly IInventoryService _inventoryService;
    private readonly IMediator _mediator;  // Can inject mediator to send follow-up requests
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository repository,
        IInventoryService inventoryService,
        IMediator mediator,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _repository = repository;
        _inventoryService = inventoryService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<int> HandleAsync(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Verify inventory
        var hasInventory = await _inventoryService.CheckAvailabilityAsync(
            request.Items,
            cancellationToken);

        if (!hasInventory)
            throw new OutOfStockException();

        // Create order
        var order = new Order(request.UserId, request.Items);
        await _repository.AddAsync(order, cancellationToken);

        // Publish notification
        await _mediator.Publish(
            new OrderPlacedNotification(order.Id, request.UserId, order.TotalAmount),
            cancellationToken);

        return order.Id;
    }
}
```

### Generic Handlers

Create generic handlers for common operations:

```csharp
public interface IEntity
{
    int Id { get; }
}

public record DeleteEntityCommand<TEntity>(int Id) : IRequest
    where TEntity : IEntity;

public class DeleteEntityCommandHandler<TEntity> : IRequestHandler<DeleteEntityCommand<TEntity>, Unit>
    where TEntity : class, IEntity
{
    private readonly IRepository<TEntity> _repository;

    public DeleteEntityCommandHandler(IRepository<TEntity> repository) => _repository = repository;

    public async Task<Unit> HandleAsync(DeleteEntityCommand<TEntity> request, CancellationToken cancellationToken)
    {
        await _repository.DeleteAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}

// Usage
await _mediator.SendAsync(new DeleteEntityCommand<Product>(productId), cancellationToken);
await _mediator.SendAsync(new DeleteEntityCommand<Customer>(customerId), cancellationToken);
```

## Architecture

### Design Patterns

**Mediator Pattern**: Decouples request senders from handlers, reducing direct dependencies between components.

**CQRS (Command Query Responsibility Segregation)**: Separates read operations (queries) from write operations (commands) for clearer code organization.

**Pipeline Pattern**: Behaviors form a pipeline around handlers, enabling composable cross-cutting concerns.

### Flow Diagrams

#### Request/Response Flow

```
Controller
    ↓
IMediator.SendAsync(request)
    ↓
IPipelineBehavior<TRequest, TResponse> (Behavior N)
    ↓
IPipelineBehavior<TRequest, TResponse> (Behavior 1)
    ↓
IRequestHandler<TRequest, TResponse>
    ↓
Response
```

#### Publish/Subscribe Flow

```
Service
    ↓
IMediator.Publish(notification)
    ↓
┌──────────────────────────────────────────────┐
│  Parallel Execution (Task.WhenAll)           │
├──────────────────────────────────────────────┤
│  INotificationPipelineBehavior<TNotification>│
│      ↓                                        │
│  INotificationHandler<TNotification> #1      │
├──────────────────────────────────────────────┤
│  INotificationPipelineBehavior<TNotification>│
│      ↓                                        │
│  INotificationHandler<TNotification> #2      │
├──────────────────────────────────────────────┤
│  INotificationPipelineBehavior<TNotification>│
│      ↓                                        │
│  INotificationHandler<TNotification> #N      │
└──────────────────────────────────────────────┘
    ↓
Complete
```

### Project Structure

```
Mediator.CQRS/
├── IMediator.cs                          # Main mediator interface
├── Mediator.cs                           # Mediator implementation
├── IRequest.cs                           # Request marker interfaces
├── IRequestHandler.cs                    # Request handler interface
├── INotification.cs                      # Notification marker interface
├── INotificationHandler.cs               # Notification handler interface
├── IPipelineBehavior.cs                  # Request pipeline behavior interface
├── INotificationPipelineBehavior.cs      # Notification pipeline behavior interface
├── Unit.cs                               # Void-like return type
└── DependencyInjection.cs                # Registration extensions
```

## Testing

### Test Statistics

- **Total Tests**: 106
- **Passed**: 106
- **Failed**: 0
- **Code Coverage**: 100% (line and branch)

### Test Categories

| Category | Test Count | Description |
|----------|------------|-------------|
| Request Handler Tests | 23 | Request/response handling, validation, cancellation |
| Notification Handler Tests | 15 | Single/multiple handlers, exception handling |
| Pipeline Behavior Tests | 18 | Request behavior execution order, short-circuiting |
| Notification Pipeline Behavior Tests | 12 | Notification behavior execution, exception handling |
| Dependency Injection Tests | 18 | Service registration, assembly scanning |
| Integration Tests | 12 | End-to-end scenarios with multiple behaviors |
| Unit Tests | 8 | Unit type, marker interfaces |

### Example Test

```csharp
[Fact]
public async Task SendAsync_WithPipelineBehavior_ExecutesInCorrectOrder()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddMediator(typeof(TestHandler).Assembly);
    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var response = await mediator.SendAsync(new TestRequest { Value = 42 });

    // Assert
    Assert.Equal(42, response.Value);
    Assert.Equal(new[] { "Behavior1", "Behavior2", "Handler" }, ExecutionOrder);
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## API Reference

### Core Interfaces

#### `IMediator`

Main entry point for sending requests and publishing notifications.

```csharp
public interface IMediator
{
    Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);

    Task Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
```

#### `IRequest<TResponse>`

Marker interface for requests that return a response.

```csharp
public interface IRequest<out TResponse> { }
```

#### `IRequest`

Marker interface for requests without a response (returns `Unit`).

```csharp
public interface IRequest : IRequest<Unit> { }
```

#### `IRequestHandler<TRequest, TResponse>`

Handler for processing requests.

```csharp
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
```

#### `IPipelineBehavior<TRequest, TResponse>`

Pipeline behavior for cross-cutting concerns on requests.

```csharp
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandler<TResponse> nextHandler,
        CancellationToken cancellationToken);
}
```

#### `INotification`

Marker interface for notifications.

```csharp
public interface INotification { }
```

#### `INotificationHandler<TNotification>`

Handler for processing notifications.

```csharp
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}
```

#### `INotificationPipelineBehavior<TNotification>`

Pipeline behavior for cross-cutting concerns on notifications.

```csharp
public interface INotificationPipelineBehavior<in TNotification>
    where TNotification : INotification
{
    Task HandleAsync(
        TNotification notification,
        NotificationHandler nextHandler,
        CancellationToken cancellationToken);
}
```

### Types

#### `Unit`

Represents a void return type for commands that don't return data.

```csharp
public readonly record struct Unit
{
    public static Unit Value { get; }
}
```

#### `RequestHandler<TResponse>`

Delegate representing the next handler in the request pipeline.

```csharp
public delegate Task<TResponse> RequestHandler<TResponse>();
```

#### `NotificationHandler`

Delegate representing the next handler in the notification pipeline.

```csharp
public delegate Task NotificationHandler();
```

### Extension Methods

#### `AddMediator`

Registers the mediator and scans assemblies for handlers and behaviors.

```csharp
public static IServiceCollection AddMediator(
    this IServiceCollection services,
    params Assembly[] assemblies)
```

**Parameters:**
- `services` - The service collection
- `assemblies` - Assemblies to scan for handlers and behaviors (at least one required)

**Returns:** The service collection for method chaining

**Throws:**
- `ArgumentNullException` - If `services` or `assemblies` is null
- `ArgumentException` - If no assemblies are provided

**Registration Details:**
- Registers `IMediator` as **scoped**
- Registers all `IRequestHandler<,>` implementations as **scoped**
- Registers all `IPipelineBehavior<,>` implementations as **scoped**
- Registers all `INotificationHandler<>` implementations as **scoped**
- Registers all `INotificationPipelineBehavior<>` implementations as **scoped**
- Only scans **public**, **non-abstract** classes

## Performance Considerations

### Service Lifetime

All services (mediator, handlers, behaviors) are registered as **scoped** by default:
- Ensures proper disposal of resources
- Aligns with typical ASP.NET Core request lifetimes
- Prevents memory leaks in web applications

### Handler Resolution

Handlers are resolved from the DI container at runtime:
- Minimal overhead using .NET's efficient service resolution
- No runtime compilation required
- Reflection is used with proper caching by the DI container

### Notification Execution

Notifications are processed in parallel:
- All handlers execute concurrently via `Task.WhenAll`
- Maximizes throughput for independent operations
- Exceptions are aggregated if multiple handlers fail

### Memory Allocation

Minimal allocations:
- `Unit` is a readonly struct (no heap allocation)
- Records encourage immutable request/response patterns
- Pipeline delegates reuse existing task infrastructure

## Best Practices

### Do

- **Use records for requests/responses** - Immutable by default, clear intent
- **Keep handlers focused** - Single responsibility per handler
- **Use pipeline behaviors for cross-cutting concerns** - Validation, logging, caching
- **Validate requests in behaviors** - Centralized validation logic
- **Use cancellation tokens** - Proper cancellation support
- **Return `Unit` for void commands** - Consistent return type pattern
- **Use notifications for side effects** - Email, logging, analytics
- **Leverage parallel notification execution** - Independent handler operations

### Don't

- **Don't inject `IMediator` into handlers to call other handlers** - Creates tight coupling, breaks SRP
- **Don't put business logic in behaviors** - Behaviors should be generic and reusable
- **Don't forget to register assemblies** - `AddMediator()` requires at least one assembly
- **Don't use notifications for request/response** - Use `IRequest<TResponse>` instead
- **Don't ignore cancellation tokens** - Always pass them through
- **Don't catch and suppress exceptions in notifications** - Let them bubble up for proper error handling

### Naming Conventions

```csharp
// Queries: GetXQuery, ListXQuery, FindXQuery
public record GetUserQuery(int UserId) : IRequest<UserDto>;

// Commands: CreateXCommand, UpdateXCommand, DeleteXCommand
public record CreateOrderCommand(int UserId, List<OrderItem> Items) : IRequest<int>;

// Notifications: XEventNotification, XChangedNotification
public record OrderPlacedNotification(int OrderId) : INotification;

// Handlers: XQueryHandler, XCommandHandler, XHandler
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto> { }

// Behaviors: XBehavior
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> { }
```

## Comparison with MediatR

| Feature | Mediator.CQRS | MediatR |
|---------|---------------|---------|
| Request/Response | ✅ | ✅ |
| Notifications | ✅ | ✅ |
| Request Pipeline Behaviors | ✅ | ✅ |
| Notification Pipeline Behaviors | ✅ | ❌ |
| Streaming | ❌ | ✅ |
| Pre/Post Request Behaviors | ❌ | ✅ |
| Dependencies | 1 | 3+ |
| Package Size | ~10 KB | ~50 KB |
| .NET Standard 2.0 | ✅ | ✅ |
| Test Coverage | 100% | ~95% |
| Complexity | Low | Medium |

**Choose Mediator.CQRS if:**
- You want a lightweight, focused implementation
- You need notification pipeline behaviors
- You prefer minimal dependencies
- You want to understand your mediator implementation

**Choose MediatR if:**
- You need streaming support (`IStreamRequest`)
- You need pre/post request processors
- You're already familiar with MediatR's ecosystem
- You need a battle-tested library with large community

## Contributing

Contributions are welcome! This project maintains high standards:

- **100% test coverage required** - All new code must be fully tested
- **No breaking changes** - Follow semantic versioning
- **Documentation required** - Update README for new features
- **Follow existing patterns** - Maintain consistency

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

**Copyright (c) 2025 Alexandros Mouratidis**

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Acknowledgments

Inspired by [MediatR](https://github.com/jbogard/MediatR) by Jimmy Bogard.

---

**Made with precision by Alexandros Mouratidis**
