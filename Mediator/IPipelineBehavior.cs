namespace Mediator;

/// <summary>
/// Defines a pipeline behavior for cross-cutting concerns (validation, logging, etc.).
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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response.</returns>
    Task<TResponse> Handle(TRequest request, RequestHandler<TResponse> nextHandler, CancellationToken cancellationToken);
}

/// <summary>
/// Delegate for the next handler in the pipeline.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <returns>The response task.</returns>
public delegate Task<TResponse> RequestHandler<TResponse>();

// Note: RequestHandler must be public because it's part of the public IPipelineBehavior interface signature
