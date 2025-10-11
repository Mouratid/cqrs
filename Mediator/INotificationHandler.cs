using System.Threading;
using System.Threading.Tasks;

namespace Mediator
{
    /// <summary>
    /// Defines a handler for a specific notification type.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification to handle.</typeparam>
    public interface INotificationHandler<in TNotification> where TNotification : INotification
    {
        /// <summary>
        /// Handles a notification.
        /// </summary>
        /// <param name="notification">The notification instance.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
    }
}
