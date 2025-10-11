using System.Threading;
using System.Threading.Tasks;

namespace Mediator
{
    /// <summary>
    /// Defines a pipeline behavior for notifications (validation, logging, etc.).
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    public interface INotificationPipelineBehavior<in TNotification>
        where TNotification : INotification
    {
        /// <summary>
        /// Handles the notification and invokes the next behavior or handler in the pipeline.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="nextHandler">The next handler in the pipeline.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HandleAsync(TNotification notification, NotificationHandler nextHandler, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Delegate for the next notification handler in the pipeline.
    /// </summary>
    /// <returns>The task.</returns>
    public delegate Task NotificationHandler();

    // Note: RequestHandler must be public because it's part of the public IPipelineBehavior interface signature
}
