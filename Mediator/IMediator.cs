using System.Threading;
using System.Threading.Tasks;

namespace Mediator
{
    /// <summary>
    /// Mediator for sending requests to handlers.
    /// </summary>
    public interface IMediator
    {
        /// <summary>
        /// Sends a request to the appropriate handler.
        /// </summary>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request to send.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The response from the handler.</returns>
        Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification;
    }
}
