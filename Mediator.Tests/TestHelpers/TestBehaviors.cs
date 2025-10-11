namespace Mediator.Tests.TestHelpers;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public static List<string> LoggedMessages { get; } = new();

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandler<TResponse> nextHandler, CancellationToken cancellationToken)
    {
        LoggedMessages.Add($"Before handling {typeof(TRequest).Name}");
        var response = await nextHandler();
        LoggedMessages.Add($"After handling {typeof(TRequest).Name}");
        return response;
    }
}

// Concrete implementations for specific request types
public class TestQueryLoggingBehavior : IPipelineBehavior<TestQuery, string>
{
    public async Task<string> HandleAsync(TestQuery request, RequestHandler<string> nextHandler, CancellationToken cancellationToken)
    {
        LoggingBehavior<TestQuery, string>.LoggedMessages.Add($"Before handling {typeof(TestQuery).Name}");
        var response = await nextHandler();
        LoggingBehavior<TestQuery, string>.LoggedMessages.Add($"After handling {typeof(TestQuery).Name}");
        return response;
    }
}

public class TestCommandLoggingBehavior : IPipelineBehavior<TestCommand, Unit>
{
    public async Task<Unit> HandleAsync(TestCommand request, RequestHandler<Unit> nextHandler, CancellationToken cancellationToken)
    {
        LoggingBehavior<TestCommand, Unit>.LoggedMessages.Add($"Before handling {typeof(TestCommand).Name}");
        var response = await nextHandler();
        LoggingBehavior<TestCommand, Unit>.LoggedMessages.Add($"After handling {typeof(TestCommand).Name}");
        return response;
    }
}

public class TestRequestWithResponseLoggingBehavior : IPipelineBehavior<TestRequestWithResponse, TestResponse>
{
    public async Task<TestResponse> HandleAsync(TestRequestWithResponse request, RequestHandler<TestResponse> nextHandler, CancellationToken cancellationToken)
    {
        LoggingBehavior<TestRequestWithResponse, TestResponse>.LoggedMessages.Add($"Before handling {typeof(TestRequestWithResponse).Name}");
        var response = await nextHandler();
        LoggingBehavior<TestRequestWithResponse, TestResponse>.LoggedMessages.Add($"After handling {typeof(TestRequestWithResponse).Name}");
        return response;
    }
}

public class TestVoidCommandLoggingBehavior : IPipelineBehavior<TestVoidCommand, Unit>
{
    public async Task<Unit> HandleAsync(TestVoidCommand request, RequestHandler<Unit> nextHandler, CancellationToken cancellationToken)
    {
        LoggingBehavior<TestVoidCommand, Unit>.LoggedMessages.Add($"Before handling {typeof(TestVoidCommand).Name}");
        var response = await nextHandler();
        LoggingBehavior<TestVoidCommand, Unit>.LoggedMessages.Add($"After handling {typeof(TestVoidCommand).Name}");
        return response;
    }
}

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandler<TResponse> nextHandler, CancellationToken cancellationToken)
    {
        if (request is TestQuery query && string.IsNullOrEmpty(query.Input))
        {
            throw new ArgumentException("Input cannot be empty");
        }

        return await nextHandler();
    }
}

public class TestQueryValidationBehavior : IPipelineBehavior<TestQuery, string>
{
    public async Task<string> HandleAsync(TestQuery request, RequestHandler<string> nextHandler, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Input))
        {
            throw new ArgumentException("Input cannot be empty");
        }

        return await nextHandler();
    }
}

public class OrderTestBehavior1<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public static List<string> ExecutionOrder { get; } = new();

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandler<TResponse> nextHandler, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("Behavior1-Before");
        var response = await nextHandler();
        ExecutionOrder.Add("Behavior1-After");
        return response;
    }
}

public class OrderTestBehavior2<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandler<TResponse> nextHandler, CancellationToken cancellationToken)
    {
        OrderTestBehavior1<TRequest, TResponse>.ExecutionOrder.Add("Behavior2-Before");
        var response = await nextHandler();
        OrderTestBehavior1<TRequest, TResponse>.ExecutionOrder.Add("Behavior2-After");
        return response;
    }
}

// Concrete implementations for order testing
public class TestQueryOrderBehavior1 : IPipelineBehavior<TestQuery, string>
{
    public async Task<string> HandleAsync(TestQuery request, RequestHandler<string> nextHandler, CancellationToken cancellationToken)
    {
        OrderTestBehavior1<TestQuery, string>.ExecutionOrder.Add("Behavior1-Before");
        var response = await nextHandler();
        OrderTestBehavior1<TestQuery, string>.ExecutionOrder.Add("Behavior1-After");
        return response;
    }
}

public class TestQueryOrderBehavior2 : IPipelineBehavior<TestQuery, string>
{
    public async Task<string> HandleAsync(TestQuery request, RequestHandler<string> nextHandler, CancellationToken cancellationToken)
    {
        OrderTestBehavior1<TestQuery, string>.ExecutionOrder.Add("Behavior2-Before");
        var response = await nextHandler();
        OrderTestBehavior1<TestQuery, string>.ExecutionOrder.Add("Behavior2-After");
        return response;
    }
}

// Abstract behavior to test that abstract classes are ignored
public abstract class AbstractTestBehavior : IPipelineBehavior<TestQuery, string>
{
    public abstract Task<string> HandleAsync(TestQuery request, RequestHandler<string> nextHandler, CancellationToken cancellationToken);
}

// Generic type definition to test that open generics are ignored
public class GenericTestBehavior<T> : IPipelineBehavior<TestQuery, string>
{
    public Task<string> HandleAsync(TestQuery request, RequestHandler<string> nextHandler, CancellationToken cancellationToken)
    {
        return nextHandler();
    }
}

// Interface implementing IPipelineBehavior to test that interfaces are ignored
public interface ITestBehaviorInterface : IPipelineBehavior<TestQuery, string>
{
}

// Struct implementing IPipelineBehavior to test that structs are ignored
public struct TestBehaviorStruct : IPipelineBehavior<TestQuery, string>
{
    public Task<string> HandleAsync(TestQuery request, RequestHandler<string> nextHandler, CancellationToken cancellationToken)
    {
        return nextHandler();
    }
}
