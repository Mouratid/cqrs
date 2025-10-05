# Mediator.CQRS

[![NuGet](https://img.shields.io/nuget/v/Mediator.CQRS.svg)](https://www.nuget.org/packages/Mediator.CQRS/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A lightweight, high-performance mediator pattern implementation for CQRS-style request/response handling with pipeline behavior support.

## Why Mediator.CQRS?

**Simple. Fast. Powerful.**

- 🚀 **Zero configuration** - Register all handlers automatically with assembly scanning
- 🎯 **Type-safe** - Compile-time checking ensures request/response type matching
- ⚡ **High performance** - Minimal overhead with efficient service resolution
- 🔌 **Pipeline behaviors** - Add cross-cutting concerns like validation, logging, caching
- 🧩 **Dependency injection first** - Built on `Microsoft.Extensions.DependencyInjection`
- 📦 **Zero dependencies** - Only requires `Microsoft.Extensions.DependencyInjection.Abstractions`
- 🎓 **Easy to learn** - Clean, intuitive API that follows SOLID principles

## Installation

```bash
dotnet add package Mediator.CQRS
```

## Quick Start

### 1. Register the mediator

```csharp
using Mediator;

var builder = WebApplication.CreateBuilder(args);

// Register mediator and scan for handlers in the current assembly
builder.Services.AddMediator(Assembly.GetExecutingAssembly());
```

### 2. Define a query

```csharp
// Request
public record GetUserQuery(int Id) : IRequest<UserResponse>;

// Response
public record UserResponse(int Id, string Name, string Email);

// Handler
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserResponse>
{
    private readonly IUserRepository _repository;

    public GetUserQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserResponse> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return new UserResponse(user.Id, user.Name, user.Email);
    }
}
```

### 3. Send the request

```csharp
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _mediator.SendAsync(new GetUserQuery(id));
        return Ok(user);
    }
}
```

## Commands vs Queries

### Query (returns data)

```csharp
// Define the query and response
public record GetProductQuery(int Id) : IRequest<ProductDto>;

public class GetProductHandler : IRequestHandler<GetProductQuery, ProductDto>
{
    public async Task<ProductDto> HandleAsync(GetProductQuery request, CancellationToken cancellationToken)
    {
        // Fetch and return data
    }
}

// Usage
var product = await _mediator.SendAsync(new GetProductQuery(42));
```

### Command (performs action, returns data)

```csharp
// Define the command and response
public record CreateProductCommand(string Name, decimal Price) : IRequest<int>;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, int>
{
    public async Task<int> HandleAsync(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Create product and return ID
        return newProductId;
    }
}

// Usage
var productId = await _mediator.SendAsync(new CreateProductCommand("Widget", 29.99m));
```

### Command (no return value)

```csharp
// Define the command using IRequest (returns Unit)
public record DeleteProductCommand(int Id) : IRequest;

public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        // Delete product
        return Unit.Value; // Indicates completion
    }
}

// Usage
await _mediator.SendAsync(new DeleteProductCommand(42));
```

## Pipeline Behaviors

Add cross-cutting concerns that execute before and after your handlers.

### Creating a Behavior

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
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
                "Handled {RequestName} in {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error handling {RequestName} after {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### Validation Behavior

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandler<TResponse> nextHandler,
        CancellationToken cancellationToken)
    {
        // Validate request
        if (request is IValidatable validatable)
        {
            var errors = validatable.Validate();
            if (errors.Any())
            {
                throw new ValidationException(string.Join(", ", errors));
            }
        }

        return await nextHandler();
    }
}
```

### Short-Circuit Behavior (Caching Example)

```csharp
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICache _cache;

    public CachingBehavior(ICache cache) => _cache = cache;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandler<TResponse> nextHandler,
        CancellationToken cancellationToken)
    {
        if (request is ICacheable cacheable)
        {
            var cacheKey = cacheable.CacheKey;

            // Try to get from cache
            if (_cache.TryGet<TResponse>(cacheKey, out var cachedResponse))
            {
                return cachedResponse; // Short-circuit!
            }

            // Execute handler and cache result
            var response = await nextHandler();
            _cache.Set(cacheKey, response, cacheable.CacheDuration);
            return response;
        }

        return await nextHandler();
    }
}
```

Behaviors are automatically discovered and registered during assembly scanning. They execute in the order they were registered.

## Advanced Scenarios

### Multiple Assemblies

```csharp
services.AddMediator(
    typeof(Program).Assembly,
    typeof(Domain.User).Assembly,
    typeof(Infrastructure.Repository).Assembly
);
```

### Request/Response Validation

```csharp
public interface IValidatable
{
    IEnumerable<string> Validate();
}

public record CreateUserCommand(string Name, string Email)
    : IRequest<int>, IValidatable
{
    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return "Name is required";

        if (!Email.Contains('@'))
            yield return "Invalid email format";
    }
}
```

### Exception Handling

```csharp
public class ExceptionHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger _logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandler<TResponse> nextHandler,
        CancellationToken cancellationToken)
    {
        try
        {
            return await nextHandler();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for {RequestType}", typeof(TRequest).Name);
            throw; // Re-throw to let middleware handle
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {RequestType}", typeof(TRequest).Name);
            throw;
        }
    }
}
```

## API Reference

### Core Interfaces

#### `IMediator`
Main entry point for sending requests.

```csharp
public interface IMediator
{
    Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);
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
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
```

#### `IPipelineBehavior<TRequest, TResponse>`
Pipeline behavior for cross-cutting concerns.

```csharp
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandler<TResponse> nextHandler,
        CancellationToken cancellationToken);
}
```

### Types

#### `Unit`
Represents a void return type.

```csharp
public readonly record struct Unit
{
    public static Unit Value { get; }
}
```

### Extension Methods

#### `AddMediator`
Registers the mediator and scans assemblies for handlers and behaviors.

```csharp
public static IServiceCollection AddMediator(
    this IServiceCollection services,
    params Assembly[] assemblies);
```

## Design Principles

### CQRS (Command Query Responsibility Segregation)
Separates read operations (queries) from write operations (commands).

- **Queries**: Return data, don't modify state
- **Commands**: Modify state, optionally return data

### Mediator Pattern
Reduces coupling between components by having them communicate through a mediator.

- Request handlers don't know about each other
- Controllers/services don't need direct dependencies on handlers
- Easy to add new requests without modifying existing code

### Pipeline Pattern
Behaviors form a pipeline around handlers.

```
Request → [Behavior 1] → [Behavior 2] → [Handler] → [Behavior 2] → [Behavior 1] → Response
```

## Performance

Mediator.CQRS is designed for high performance:

- **Scoped service lifetime** prevents memory leaks and ensures proper disposal
- **Efficient service resolution** using .NET's built-in DI container
- **Minimal allocations** with struct-based Unit type
- **No runtime compilation** - uses reflection with proper caching by the DI container

## Best Practices

### ✅ Do

- Use records for requests and responses (immutable by default)
- Keep handlers focused on a single responsibility
- Use pipeline behaviors for cross-cutting concerns
- Validate requests in a validation behavior
- Use cancellation tokens for long-running operations
- Return `Unit` for commands that don't need to return data

### ❌ Don't

- Don't inject `IMediator` into handlers (creates circular dependencies)
- Don't use mediator to call other handlers directly (breaks SRP)
- Don't put business logic in behaviors (they should be generic)
- Don't forget to register assemblies with `AddMediator()`

## Comparison with MediatR

| Feature | Mediator.CQRS | MediatR |
|---------|---------------|---------|
| Request/Response | ✅ | ✅ |
| Pipeline Behaviors | ✅ | ✅ |
| Notifications | ❌ | ✅ |
| Streams | ❌ | ✅ |
| Dependencies | 1 | 3+ |
| Size | ~5 KB | ~50 KB |
| Performance | High | High |
| Complexity | Low | Medium |

**When to use Mediator.CQRS:**
- You want a simple, focused mediator implementation
- You only need request/response pattern (no pub/sub)
- You want minimal dependencies
- You prefer understanding your dependencies

**When to use MediatR:**
- You need notifications (pub/sub pattern)
- You need streaming support
- You're already familiar with MediatR's API

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

Inspired by [MediatR](https://github.com/jbogard/MediatR) by Jimmy Bogard.
