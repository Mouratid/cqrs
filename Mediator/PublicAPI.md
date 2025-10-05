# Mediator Library - Public API

## Overview
This library provides a mediator pattern implementation for CQRS-style request/response handling with pipeline behavior support.

## Public API Surface

### Interfaces

#### `IMediator`
Main entry point for sending requests.
```csharp
public interface IMediator
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
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
Pipeline behavior for cross-cutting concerns (validation, logging, etc.).
```csharp
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandler<TResponse> nextHandler, CancellationToken cancellationToken);
}
```

### Delegates

#### `RequestHandler<TResponse>`
Delegate for the next handler in the pipeline.
```csharp
public delegate Task<TResponse> RequestHandler<TResponse>();
```

### Types

#### `Unit`
Represents a void return type for requests without a response.
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
public static class DependencyInjection
{
    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies);
}
```

## Internal Implementation Details

The following types are internal and not part of the public API:
- `Mediator` class (implementation of `IMediator`)

## Usage

### Registration
```csharp
services.AddMediator(Assembly.GetExecutingAssembly());
```

### Query Example
```csharp
public record GetUserQuery(int Id) : IRequest<User>;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public Task<User> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}

// Usage
var user = await mediator.SendAsync(new GetUserQuery(1));
```

### Command Example (no response)
```csharp
public record DeleteUserCommand(int Id) : IRequest;

public class DeleteUserHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    public Task<Unit> HandleAsync(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // Implementation
        return Task.FromResult(Unit.Value);
    }
}

// Usage
await mediator.SendAsync(new DeleteUserCommand(1));
```

### Pipeline Behavior Example
```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandler<TResponse> nextHandler, CancellationToken cancellationToken)
    {
        // Before
        var response = await nextHandler();
        // After
        return response;
    }
}
```
