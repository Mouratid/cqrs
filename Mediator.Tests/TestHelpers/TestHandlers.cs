namespace Mediator.Tests.TestHelpers;

public class TestQueryHandler : IRequestHandler<TestQuery, string>
{
    public Task<string> HandleAsync(TestQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Handled: {request.Input}");
    }
}

public class TestCommandHandler : IRequestHandler<TestCommand, Unit>
{
    public static int LastValue { get; set; }

    public Task<Unit> HandleAsync(TestCommand request, CancellationToken cancellationToken)
    {
        LastValue = request.Value;
        return Task.FromResult(Unit.Value);
    }
}

public class TestResponseHandler : IRequestHandler<TestRequestWithResponse, TestResponse>
{
    public Task<TestResponse> HandleAsync(TestRequestWithResponse request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestResponse
        {
            Message = $"Processed: {request.Input}",
            Value = request.Number * 2
        });
    }
}

public class TestVoidCommandHandler : IRequestHandler<TestVoidCommand, Unit>
{
    public static string LastAction { get; set; } = string.Empty;

    public Task<Unit> HandleAsync(TestVoidCommand request, CancellationToken cancellationToken)
    {
        LastAction = request.Action;
        return Task.FromResult(Unit.Value);
    }
}

public class ThrowingHandler : IRequestHandler<ThrowingQuery, string>
{
    public Task<string> HandleAsync(ThrowingQuery request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Test exception");
    }
}

// Abstract handler to test that abstract classes are ignored
public abstract class AbstractTestHandler : IRequestHandler<TestQuery, string>
{
    public abstract Task<string> HandleAsync(TestQuery request, CancellationToken cancellationToken);
}

// Generic type definition to test that open generics are ignored
public class GenericTestHandler<T> : IRequestHandler<TestQuery, string>
{
    public Task<string> HandleAsync(TestQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Generic handler: {typeof(T).Name}");
    }
}

// Interface implementing IRequestHandler to test that interfaces are ignored
public interface ITestHandlerInterface : IRequestHandler<TestQuery, string>
{
}

// Struct implementing IRequestHandler to test that structs are ignored
public struct TestHandlerStruct : IRequestHandler<TestQuery, string>
{
    public Task<string> HandleAsync(TestQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult("Struct handler");
    }
}
