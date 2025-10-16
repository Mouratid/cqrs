using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Mediator.Tests.TestHelpers;

public static class StreamBehaviorTracker
{
    public static List<string> ExecutionOrder { get; } = new();
    public static void Reset() => ExecutionOrder.Clear();
}

public class StreamLoggingBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    public async IAsyncEnumerable<TResponse> HandleAsync(
        TRequest request,
        StreamRequestHandler<TResponse> nextHandler,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        StreamBehaviorTracker.ExecutionOrder.Add("StreamLoggingBehavior-Before");

        await foreach (var item in nextHandler().WithCancellation(cancellationToken))
        {
            StreamBehaviorTracker.ExecutionOrder.Add($"StreamLoggingBehavior-Item");
            yield return item;
        }

        StreamBehaviorTracker.ExecutionOrder.Add("StreamLoggingBehavior-After");
    }
}

public class StreamTransformBehavior : IStreamPipelineBehavior<TestStreamRequestWithBehavior, string>
{
    public async IAsyncEnumerable<string> HandleAsync(
        TestStreamRequestWithBehavior request,
        StreamRequestHandler<string> nextHandler,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        StreamBehaviorTracker.ExecutionOrder.Add("StreamTransformBehavior-Before");

        await foreach (var item in nextHandler().WithCancellation(cancellationToken))
        {
            StreamBehaviorTracker.ExecutionOrder.Add($"StreamTransformBehavior-Item");
            yield return item.ToUpper();
        }

        StreamBehaviorTracker.ExecutionOrder.Add("StreamTransformBehavior-After");
    }
}

public class StreamFilterBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    private readonly Func<TResponse, bool> _predicate;

    public StreamFilterBehavior(Func<TResponse, bool> predicate)
    {
        _predicate = predicate;
    }

    public async IAsyncEnumerable<TResponse> HandleAsync(
        TRequest request,
        StreamRequestHandler<TResponse> nextHandler,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in nextHandler().WithCancellation(cancellationToken))
        {
            if (_predicate(item))
            {
                yield return item;
            }
        }
    }
}

public class StreamShortCircuitBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    private readonly IAsyncEnumerable<TResponse> _shortCircuitStream;

    public StreamShortCircuitBehavior(IAsyncEnumerable<TResponse> shortCircuitStream)
    {
        _shortCircuitStream = shortCircuitStream;
    }

    public async IAsyncEnumerable<TResponse> HandleAsync(
        TRequest request,
        StreamRequestHandler<TResponse> nextHandler,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        StreamBehaviorTracker.ExecutionOrder.Add("StreamShortCircuit");

        await foreach (var item in _shortCircuitStream.WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }
}
