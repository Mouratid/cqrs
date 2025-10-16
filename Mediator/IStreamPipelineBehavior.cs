using System.Collections.Generic;
using System.Threading;

namespace Mediator;

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
