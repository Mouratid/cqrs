using System.Threading;
using System.Threading.Tasks;

namespace Mediator;

/// <summary>
/// Defines a handler for a request.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request asynchronously.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response.</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
