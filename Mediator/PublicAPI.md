# Mediator.CQRS - Public API Reference

## Overview

This library provides a complete mediator pattern implementation for CQRS-style architectures, supporting request/response handling, streaming data, and publish/subscribe notifications with extensible pipeline behaviors.

**Version**: 1.0.0
**Target Framework**: .NET Standard 2.0
**Dependencies**: Microsoft.Extensions.DependencyInjection.Abstractions

---

## Public API Surface

### Core Interfaces

#### `IMediator`

Main entry point for sending requests and publishing notifications.

```csharp
namespace Mediator
{
    public interface IMediator
    {
        /// <summary>
        /// Sends a request to the appropriate handler and returns the response.
        /// </summary>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request to send.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The response from the handler.</returns>
        /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no handler is registered for the request type.</exception>
        Task<TResponse> SendAsync<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a streaming request to the appropriate handler and returns an async stream of responses.
        /// </summary>
        /// <typeparam name="TResponse">The type of items in the stream.</typeparam>
        /// <param name="request">The streaming request to send.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the stream enumeration.</param>
        /// <returns>An async stream of responses from the handler.</returns>
        /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no handler is registered for the request type.</exception>
        IAsyncEnumerable<TResponse> SendStreamAsync<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a notification to all registered handlers.
        /// </summary>
        /// <typeparam name="TNotification">The notification type.</typeparam>
        /// <param name="notification">The notification to publish.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when notification is null.</exception>
        /// <exception cref="AggregateException">Thrown when multiple handlers fail.</exception>
        Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification;
    }
}
```

**Behavior**:
- `SendAsync`: Routes the request to exactly one handler. Throws `InvalidOperationException` if no handler is registered.
- `SendStreamAsync`: Routes the streaming request to exactly one handler and returns an async stream. Throws `InvalidOperationException` if no handler is registered. The stream supports cancellation via the provided cancellation token.
- `Publish`: Invokes all registered handlers for the notification type in parallel using `Task.WhenAll`. If multiple handlers throw exceptions, they are aggregated into an `AggregateException`.

---

### Request/Response Interfaces

#### `IRequest<TResponse>`

Marker interface for requests that return a response.

```csharp
namespace Mediator
{
    /// <summary>
    /// Marker interface for requests that return a response.
    /// </summary>
    /// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
    public interface IRequest<out TResponse> { }
}
```

**Usage**: Implement this interface on request types (queries, commands) that return data.

#### `IRequest`

Marker interface for requests without a response (returns `Unit`).

```csharp
namespace Mediator
{
    /// <summary>
    /// Marker interface for requests that do not return a meaningful response.
    /// </summary>
    public interface IRequest : IRequest<Unit> { }
}
```

**Usage**: Implement this interface on command types that perform actions without returning data.

#### `IRequestHandler<TRequest, TResponse>`

Handler for processing requests.

```csharp
namespace Mediator
{
    /// <summary>
    /// Handles a request and returns a response.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    public interface IRequestHandler<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        /// <summary>
        /// Handles the request and returns a response.
        /// </summary>
        /// <param name="request">The request to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The response.</returns>
        Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }
}
```

**Service Lifetime**: Registered as **scoped** by default.

---

### Streaming Interfaces

#### `IStreamRequest<TResponse>`

Marker interface for streaming requests that return an async stream of responses.

```csharp
namespace Mediator
{
    /// <summary>
    /// Marker interface for streaming requests.
    /// </summary>
    /// <typeparam name="TResponse">The type of items in the stream.</typeparam>
    public interface IStreamRequest<out TResponse> { }
}
```

**Usage**: Implement this interface on request types that need to return multiple items over time via streaming.

**Use Cases**:
- Large dataset processing (pagination, bulk data export)
- Real-time data feeds (stock prices, sensor data, live updates)
- Progressive data loading (infinite scroll, chunked processing)
- Server-sent events or push notifications

#### `IStreamRequestHandler<TRequest, TResponse>`

Handler for processing streaming requests.

```csharp
namespace Mediator
{
    /// <summary>
    /// Defines a handler for a streaming request.
    /// </summary>
    /// <typeparam name="TRequest">The streaming request type.</typeparam>
    /// <typeparam name="TResponse">The type of items in the stream.</typeparam>
    public interface IStreamRequestHandler<in TRequest, out TResponse>
        where TRequest : IStreamRequest<TResponse>
    {
        /// <summary>
        /// Handles the streaming request asynchronously.
        /// </summary>
        /// <param name="request">The request to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>An async stream of responses.</returns>
        IAsyncEnumerable<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }
}
```

**Service Lifetime**: Registered as **scoped** by default.
**Implementation Note**: Use `[EnumeratorCancellation]` attribute on the cancellation token parameter for proper async enumerable cancellation support.

---

### Streaming Pipeline Behavior Interfaces

#### `IStreamPipelineBehavior<TRequest, TResponse>`

Pipeline behavior for cross-cutting concerns on streaming requests.

```csharp
namespace Mediator
{
    /// <summary>
    /// Delegate representing the next handler in the streaming pipeline.
    /// </summary>
    /// <typeparam name="TResponse">The type of items in the stream.</typeparam>
    /// <returns>An async stream of responses.</returns>
    public delegate IAsyncEnumerable<TResponse> StreamRequestHandler<out TResponse>();

    /// <summary>
    /// Defines a behavior that wraps streaming request handlers.
    /// Behaviors execute in reverse registration order (last registered executes first).
    /// </summary>
    /// <typeparam name="TRequest">The streaming request type.</typeparam>
    /// <typeparam name="TResponse">The type of items in the stream.</typeparam>
    public interface IStreamPipelineBehavior<in TRequest, TResponse>
        where TRequest : IStreamRequest<TResponse>
    {
        /// <summary>
        /// Handles the streaming request, optionally transforming the stream or short-circuiting.
        /// </summary>
        /// <param name="request">The request being handled.</param>
        /// <param name="nextHandler">The next handler in the pipeline.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>An async stream of responses.</returns>
        IAsyncEnumerable<TResponse> HandleAsync(
            TRequest request,
            StreamRequestHandler<TResponse> nextHandler,
            CancellationToken cancellationToken);
    }
}
```

**Service Lifetime**: Registered as **scoped** by default.
**Execution Order**: Behaviors execute in **reverse registration order** (last registered executes first).
**Short-Circuiting**: Behaviors can skip calling `nextHandler()` to short-circuit the pipeline (e.g., cached streams).
**Stream Operations**: Behaviors can:
- Transform stream items (mapping, filtering, enrichment)
- Buffer or batch stream items
- Add logging or metrics for stream consumption
- Implement caching or memoization
- Handle errors and retry logic
- Apply rate limiting or throttling

---

### Notification Interfaces

#### `INotification`

Marker interface for notifications.

```csharp
namespace Mediator
{
    /// <summary>
    /// Marker interface for notifications that can be published to multiple handlers.
    /// </summary>
    public interface INotification { }
}
```

**Usage**: Implement this interface on notification types that represent domain events or side-effects.

#### `INotificationHandler<TNotification>`

Handler for processing notifications.

```csharp
namespace Mediator
{
    /// <summary>
    /// Handles a notification.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    public interface INotificationHandler<in TNotification>
        where TNotification : INotification
    {
        /// <summary>
        /// Handles the notification.
        /// </summary>
        /// <param name="notification">The notification to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
    }
}
```

**Service Lifetime**: Registered as **scoped** by default.
**Execution**: Multiple handlers for the same notification execute **in parallel**.

---

### Pipeline Behavior Interfaces

#### `IPipelineBehavior<TRequest, TResponse>`

Pipeline behavior for cross-cutting concerns on requests.

```csharp
namespace Mediator
{
    /// <summary>
    /// Defines a pipeline behavior for requests (validation, logging, caching, etc.).
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    public interface IPipelineBehavior<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        /// <summary>
        /// Handles the request and invokes the next behavior or handler in the pipeline.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="nextHandler">The next handler in the pipeline.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The response.</returns>
        Task<TResponse> HandleAsync(
            TRequest request,
            RequestHandler<TResponse> nextHandler,
            CancellationToken cancellationToken);
    }
}
```

**Service Lifetime**: Registered as **scoped** by default.
**Execution Order**: Behaviors execute in **reverse registration order** (last registered executes first).
**Short-Circuiting**: Behaviors can skip calling `nextHandler()` to short-circuit the pipeline (e.g., caching).

#### `INotificationPipelineBehavior<TNotification>`

Pipeline behavior for cross-cutting concerns on notifications.

```csharp
namespace Mediator
{
    /// <summary>
    /// Defines a pipeline behavior for notifications (validation, logging, retry, etc.).
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    public interface INotificationPipelineBehavior<in TNotification>
        where TNotification : INotification
    {
        /// <summary>
        /// Handles the notification and invokes the next behavior or handler in the pipeline.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="nextHandler">The next handler in the pipeline.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HandleAsync(
            TNotification notification,
            NotificationHandler nextHandler,
            CancellationToken cancellationToken);
    }
}
```

**Service Lifetime**: Registered as **scoped** by default.
**Execution Order**: Behaviors execute in **reverse registration order** (last registered executes first).
**Scope**: Each notification handler is wrapped independently by the pipeline.

---

### Delegates

#### `RequestHandler<TResponse>`

Delegate representing the next handler in the request pipeline.

```csharp
namespace Mediator
{
    /// <summary>
    /// Delegate for the next request handler in the pipeline.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <returns>A task containing the response.</returns>
    public delegate Task<TResponse> RequestHandler<TResponse>();
}
```

**Usage**: Invoke this delegate in `IPipelineBehavior<,>.HandleAsync()` to continue the pipeline.

#### `NotificationHandler`

Delegate representing the next handler in the notification pipeline.

```csharp
namespace Mediator
{
    /// <summary>
    /// Delegate for the next notification handler in the pipeline.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public delegate Task NotificationHandler();
}
```

**Usage**: Invoke this delegate in `INotificationPipelineBehavior<>.HandleAsync()` to continue the pipeline.

---

### Types

#### `Unit`

Represents a void return type for requests without a response.

```csharp
namespace Mediator
{
    /// <summary>
    /// Represents a void-like return type for requests that do not return data.
    /// </summary>
    public readonly record struct Unit
    {
        /// <summary>
        /// Gets the singleton value of Unit.
        /// </summary>
        public static Unit Value { get; }
    }
}
```

**Usage**: Return `Unit.Value` from handlers that don't produce meaningful output.
**Performance**: Implemented as a readonly struct to avoid heap allocations.

---

### Extension Methods

#### `AddMediator`

Registers the mediator and scans assemblies for handlers and behaviors.

```csharp
namespace Mediator
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers the mediator and scans the provided assemblies for request handlers,
        /// notification handlers, and pipeline behaviors.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">Assemblies to scan for handlers. At least one assembly must be provided.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services or assemblies is null.</exception>
        /// <exception cref="ArgumentException">Thrown when no assemblies are provided.</exception>
        public static IServiceCollection AddMediator(
            this IServiceCollection services,
            params Assembly[] assemblies);
    }
}
```

**Behavior**:
- Registers `IMediator` as **scoped**
- Scans all provided assemblies for:
  - `IRequestHandler<,>` implementations → registered as **scoped**
  - `IStreamRequestHandler<,>` implementations → registered as **scoped**
  - `INotificationHandler<>` implementations → registered as **scoped**
  - `IPipelineBehavior<,>` implementations → registered as **scoped**
  - `IStreamPipelineBehavior<,>` implementations → registered as **scoped**
  - `INotificationPipelineBehavior<>` implementations → registered as **scoped**
- Only scans **public**, **non-abstract**, **non-generic type definitions** classes

**Example**:
```csharp
services.AddMediator(
    typeof(Program).Assembly,
    typeof(Application.CreateOrderCommand).Assembly
);
```

---

## Internal Implementation Details

The following types are internal and **not** part of the public API:
- `Mediator` class (implementation of `IMediator`)

---

## Usage Examples

### Request/Response Pattern

#### Query Example

```csharp
using Mediator;

// Define query and response
public record GetUserQuery(int UserId) : IRequest<UserDto>;
public record UserDto(int Id, string Name, string Email);

// Define handler
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    private readonly IUserRepository _repository;

    public GetUserQueryHandler(IUserRepository repository) => _repository = repository;

    public async Task<UserDto> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.UserId, cancellationToken);
        return new UserDto(user.Id, user.Name, user.Email);
    }
}

// Usage
var user = await mediator.SendAsync(new GetUserQuery(42), cancellationToken);
```

#### Command with Response

```csharp
// Define command and response
public record CreateOrderCommand(int UserId, List<OrderItem> Items) : IRequest<int>;

// Define handler
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, int>
{
    public async Task<int> HandleAsync(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Create order and return ID
        var orderId = await _repository.CreateAsync(request.UserId, request.Items, cancellationToken);
        return orderId;
    }
}

// Usage
var orderId = await mediator.SendAsync(new CreateOrderCommand(userId, items), cancellationToken);
```

#### Command without Response

```csharp
// Define command (no response)
public record DeleteUserCommand(int UserId) : IRequest;

// Define handler
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        await _repository.DeleteAsync(request.UserId, cancellationToken);
        return Unit.Value;
    }
}

// Usage
await mediator.SendAsync(new DeleteUserCommand(userId), cancellationToken);
```

---

### Streaming Pattern

#### Streaming Query Example

```csharp
using Mediator;
using System.Runtime.CompilerServices;

// Define streaming request
public record GetProductStreamQuery(int CategoryId) : IStreamRequest<ProductDto>;

public record ProductDto(int Id, string Name, decimal Price);

// Define handler
public class GetProductStreamQueryHandler : IStreamRequestHandler<GetProductStreamQuery, ProductDto>
{
    private readonly IProductRepository _repository;

    public GetProductStreamQueryHandler(IProductRepository repository) => _repository = repository;

    public async IAsyncEnumerable<ProductDto> HandleAsync(
        GetProductStreamQuery request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var page = 0;
        while (true)
        {
            var products = await _repository.GetPageAsync(request.CategoryId, page++, 100, cancellationToken);
            if (!products.Any()) break;

            foreach (var product in products)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new ProductDto(product.Id, product.Name, product.Price);
            }
        }
    }
}

// Usage - consume stream
await foreach (var product in mediator.SendStreamAsync(
    new GetProductStreamQuery(categoryId),
    cancellationToken))
{
    Console.WriteLine($"{product.Name}: ${product.Price}");
}
```

#### Real-time Streaming Example

```csharp
// Define streaming request for live data
public record GetLiveStockPricesQuery(string Symbol) : IStreamRequest<StockPrice>;

public record StockPrice(string Symbol, decimal Price, DateTime Timestamp);

// Define handler
public class GetLiveStockPricesQueryHandler : IStreamRequestHandler<GetLiveStockPricesQuery, StockPrice>
{
    private readonly IStockPriceService _service;

    public GetLiveStockPricesQueryHandler(IStockPriceService service) => _service = service;

    public async IAsyncEnumerable<StockPrice> HandleAsync(
        GetLiveStockPricesQuery request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Stream live updates until cancelled
        await foreach (var price in _service.SubscribeToPrices(request.Symbol, cancellationToken))
        {
            yield return new StockPrice(request.Symbol, price.Value, DateTime.UtcNow);
        }
    }
}

// Usage with cancellation
var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

await foreach (var price in mediator.SendStreamAsync(
    new GetLiveStockPricesQuery("AAPL"),
    cts.Token))
{
    Console.WriteLine($"{price.Symbol}: ${price.Price} at {price.Timestamp}");
}
```

#### Stream Pipeline Behavior Example

```csharp
// Logging behavior for streams
public class StreamLoggingBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    private readonly ILogger<StreamLoggingBehavior<TRequest, TResponse>> _logger;

    public StreamLoggingBehavior(ILogger<StreamLoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<TResponse> HandleAsync(
        TRequest request,
        StreamRequestHandler<TResponse> nextHandler,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Starting stream: {RequestName}", requestName);

        var count = 0;
        await foreach (var item in nextHandler().WithCancellation(cancellationToken))
        {
            count++;
            yield return item;
        }

        _logger.LogInformation("Completed stream: {RequestName}, Items: {Count}", requestName, count);
    }
}

// Transform behavior for streams
public class StreamTransformBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    public async IAsyncEnumerable<TResponse> HandleAsync(
        TRequest request,
        StreamRequestHandler<TResponse> nextHandler,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in nextHandler().WithCancellation(cancellationToken))
        {
            // Transform or filter each item
            var transformed = TransformItem(item);
            if (ShouldInclude(transformed))
            {
                yield return transformed;
            }
        }
    }

    private TResponse TransformItem(TResponse item) => item; // Custom transformation
    private bool ShouldInclude(TResponse item) => true; // Custom filtering
}
```

---

### Publish/Subscribe Pattern

#### Notification Example

```csharp
// Define notification
public record OrderPlacedNotification(int OrderId, int UserId, decimal TotalAmount) : INotification;

// Define multiple handlers
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

// Usage - both handlers execute in parallel
await mediator.Publish(
    new OrderPlacedNotification(orderId, userId, totalAmount),
    cancellationToken);
```

#### Exception Handling

```csharp
try
{
    await mediator.Publish(notification, cancellationToken);
}
catch (AggregateException ex) when (ex.InnerExceptions.Count > 1)
{
    // Multiple handlers failed
    foreach (var innerException in ex.InnerExceptions)
    {
        _logger.LogError(innerException, "Notification handler failed");
    }
}
catch (Exception ex)
{
    // Single handler failed
    _logger.LogError(ex, "Notification handler failed");
}
```

---

### Pipeline Behaviors

#### Request Logging Behavior

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
        _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);

        var response = await nextHandler();

        _logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);

        return response;
    }
}
```

#### Request Validation Behavior

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
```

#### Request Caching Behavior (Short-Circuiting)

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

    public CachingBehavior(IDistributedCache cache) => _cache = cache;

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandler<TResponse> nextHandler,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheable cacheable)
            return await nextHandler();

        var cachedValue = await _cache.GetStringAsync(cacheable.CacheKey, cancellationToken);
        if (cachedValue != null)
        {
            // Short-circuit: don't call nextHandler()
            return JsonSerializer.Deserialize<TResponse>(cachedValue)!;
        }

        var response = await nextHandler();
        await _cache.SetStringAsync(
            cacheable.CacheKey,
            JsonSerializer.Serialize(response),
            cancellationToken);

        return response;
    }
}
```

#### Notification Logging Behavior

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
        _logger.LogInformation("Processing {NotificationName}", typeof(TNotification).Name);

        try
        {
            await nextHandler();
            _logger.LogInformation("Processed {NotificationName}", typeof(TNotification).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {NotificationName}", typeof(TNotification).Name);
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
                _logger.LogWarning(ex, "Retry attempt {Attempt}/{MaxRetries}", attempt, MaxRetries);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
            }
        }
    }
}
```

---

## Registration

### Basic Registration

```csharp
using Mediator;

var builder = WebApplication.CreateBuilder(args);

// Register mediator and scan current assembly
builder.Services.AddMediator(Assembly.GetExecutingAssembly());
```

### Multiple Assembly Registration

```csharp
builder.Services.AddMediator(
    typeof(Program).Assembly,                    // API layer
    typeof(Application.Commands).Assembly,       // Application layer
    typeof(Infrastructure.Repositories).Assembly // Infrastructure layer
);
```

### Custom Service Lifetime Override

```csharp
// Register mediator with default scoped lifetime
services.AddMediator(Assembly.GetExecutingAssembly());

// Override specific handler to singleton
services.AddSingleton<IRequestHandler<GetCachedDataQuery, CachedData>, GetCachedDataQueryHandler>();
```

---

## Behavior Execution Model

### Request Pipeline Execution Order

Behaviors execute in **reverse registration order**:

```csharp
// These behaviors are discovered during assembly scanning:
// - TransactionBehavior
// - ValidationBehavior
// - LoggingBehavior

// Execution flow:
Request
    → TransactionBehavior (last registered, executes first)
        → ValidationBehavior
            → LoggingBehavior
                → Handler
            ← LoggingBehavior
        ← ValidationBehavior
    ← TransactionBehavior
← Response
```

### Notification Pipeline Execution

Each notification handler is wrapped **independently** by pipeline behaviors:

```csharp
Publish(notification)
    ↓
Task.WhenAll(
    Pipeline → Handler #1,  // Runs in parallel
    Pipeline → Handler #2,  // Runs in parallel
    Pipeline → Handler #3   // Runs in parallel
)
    ↓
Complete (or AggregateException if multiple handlers fail)
```

---

## Exception Handling

### Request Exceptions

Exceptions thrown by handlers or behaviors propagate directly to the caller:

```csharp
try
{
    var result = await mediator.SendAsync(new GetUserQuery(userId), cancellationToken);
}
catch (UserNotFoundException ex)
{
    // Handle specific exception
}
catch (Exception ex)
{
    // Handle general exception
}
```

### Notification Exceptions

**Single Handler Fails**: Exception propagates directly.

**Multiple Handlers Fail**: All exceptions are collected into an `AggregateException`:

```csharp
try
{
    await mediator.Publish(notification, cancellationToken);
}
catch (AggregateException ex) when (ex.InnerExceptions.Count > 1)
{
    // Multiple handlers threw exceptions
    foreach (var inner in ex.InnerExceptions)
    {
        _logger.LogError(inner, "Handler failed");
    }
}
catch (Exception ex)
{
    // Single handler threw exception
    _logger.LogError(ex, "Handler failed");
}
```

---

## Performance Characteristics

### Service Lifetime

- **Mediator**: Scoped
- **Handlers**: Scoped (default)
- **Behaviors**: Scoped (default)

**Rationale**: Aligns with typical ASP.NET Core request lifetimes and ensures proper disposal of resources.

### Handler Resolution

- Handlers are resolved from the DI container at runtime using reflection
- .NET's DI container caches type information for efficient resolution
- No runtime code generation or compilation

### Notification Execution

- All handlers execute **in parallel** via `Task.WhenAll`
- Maximizes throughput for independent operations
- Exceptions are aggregated if multiple handlers fail

### Memory Allocation

- `Unit` is a readonly struct (stack-allocated, no heap pressure)
- Records encourage immutable patterns
- Pipeline delegates reuse existing task infrastructure

---

## API Stability

This is the **v1.0.0** release. The public API is stable and follows semantic versioning:

- **Major version** changes indicate breaking changes
- **Minor version** changes indicate new features (backward compatible)
- **Patch version** changes indicate bug fixes

---

## See Also

- [README.md](../README.md) - Complete documentation with examples
- [LICENSE](../LICENSE) - MIT License
- [NuGet Package](https://www.nuget.org/packages/Mediator.CQRS/)

---

**Copyright (c) 2025 Alexandros Mouratidis**
Licensed under the MIT License
