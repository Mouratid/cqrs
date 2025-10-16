using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Mediator.Tests.TestHelpers;

public class TestStreamRequestHandler : IStreamRequestHandler<TestStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(
        TestStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 1; i <= request.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(1, cancellationToken);
            yield return i;
        }
    }
}

public class TestStreamRequestWithBehaviorHandler : IStreamRequestHandler<TestStreamRequestWithBehavior, string>
{
    public async IAsyncEnumerable<string> HandleAsync(
        TestStreamRequestWithBehavior request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 1; i <= request.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(1, cancellationToken);
            yield return $"Item-{i}";
        }
    }
}

public class EmptyStreamRequestHandler : IStreamRequestHandler<EmptyStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(
        EmptyStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        yield break;
    }
}

public class StreamRequestThrowsExceptionHandler : IStreamRequestHandler<StreamRequestThrowsException, int>
{
    public async IAsyncEnumerable<int> HandleAsync(
        StreamRequestThrowsException request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return 1;
        await Task.Delay(1, cancellationToken);
        throw new InvalidOperationException("Stream error");
    }
}
