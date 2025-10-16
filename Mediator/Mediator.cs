using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator
{
    internal sealed class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;
        public Mediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();
            var responseType = typeof(TResponse);
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);

            // Get the handler from DI
            var handler = _serviceProvider.GetService(handlerType)
                ?? throw new InvalidOperationException($"No handler registered for request type '{requestType.Name}'");

            // Get behaviors from DI
            var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);
            var behaviors = _serviceProvider.GetServices(behaviorType).Reverse().ToList();

            // Build the pipeline
            RequestHandler<TResponse> handlerDelegate = async () =>
            {
                var handleMethod = handlerType.GetMethod(nameof(IRequestHandler<IRequest<object>, object>.HandleAsync));
                var result = handleMethod.Invoke(handler, new object[] { request, cancellationToken });
                return await ((Task<TResponse>)result).ConfigureAwait(false);
            };

            // Wrap handler with behaviors
            foreach (var behavior in behaviors)
            {
                var currentDelegate = handlerDelegate;
                var behaviorHandleMethod = behaviorType.GetMethod(nameof(IPipelineBehavior<IRequest<object>, object>.HandleAsync));

                handlerDelegate = () =>
                {
                    var result = behaviorHandleMethod.Invoke(behavior, new object[] { request, currentDelegate, cancellationToken });

                    return (Task<TResponse>)result;
                };
            }

            return await handlerDelegate().ConfigureAwait(false);
        }

        public IAsyncEnumerable<TResponse> SendStreamAsync<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            return SendStreamAsyncCore(request, cancellationToken);
        }

        private async IAsyncEnumerable<TResponse> SendStreamAsyncCore<TResponse>(
            IStreamRequest<TResponse> request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var requestType = request.GetType();
            var responseType = typeof(TResponse);
            var handlerType = typeof(IStreamRequestHandler<,>).MakeGenericType(requestType, responseType);

            // Get the handler from DI
            var handler = _serviceProvider.GetService(handlerType)
                ?? throw new InvalidOperationException($"No stream handler registered for request type '{requestType.Name}'");

            // Get behaviors from DI
            var behaviorType = typeof(IStreamPipelineBehavior<,>).MakeGenericType(requestType, responseType);
            var behaviors = _serviceProvider.GetServices(behaviorType).Reverse().ToList();

            // Build the pipeline
            StreamRequestHandler<TResponse> handlerDelegate = () =>
            {
                var handleMethod = handlerType.GetMethod(nameof(IStreamRequestHandler<IStreamRequest<object>, object>.HandleAsync));
                var result = handleMethod.Invoke(handler, new object[] { request, cancellationToken });
                return (IAsyncEnumerable<TResponse>)result;
            };

            // Wrap handler with behaviors
            foreach (var behavior in behaviors)
            {
                var currentDelegate = handlerDelegate;
                var behaviorHandleMethod = behaviorType.GetMethod(nameof(IStreamPipelineBehavior<IStreamRequest<object>, object>.HandleAsync));

                handlerDelegate = () =>
                {
                    var result = behaviorHandleMethod.Invoke(behavior, new object[] { request, currentDelegate, cancellationToken });
                    return (IAsyncEnumerable<TResponse>)result;
                };
            }

            // Execute the pipeline and yield results
            await foreach (var item in handlerDelegate().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }

        public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            if (notification is null) throw new ArgumentNullException(nameof(notification));

            // Get all handlers for this notification type
            var handlers = _serviceProvider.GetServices<INotificationHandler<TNotification>>().ToList();

            // Get behaviors from DI
            var behaviorType = typeof(INotificationPipelineBehavior<>).MakeGenericType(typeof(TNotification));
            var behaviors = _serviceProvider.GetServices(behaviorType).Reverse().ToList();

            // Build the pipeline for each handler
            var tasks = handlers.Select(handler =>
            {
                // Build the pipeline for this handler
                NotificationHandler handlerDelegate = () => handler.HandleAsync(notification, cancellationToken);

                // Wrap handler with behaviors
                foreach (var behavior in behaviors)
                {
                    var currentDelegate = handlerDelegate;
                    var behaviorHandleMethod = behaviorType.GetMethod(nameof(INotificationPipelineBehavior<INotification>.HandleAsync));

                    handlerDelegate = () =>
                    {
                        var result = behaviorHandleMethod.Invoke(behavior, new object[] { notification, currentDelegate, cancellationToken });
                        return (Task)result;
                    };
                }

                return handlerDelegate();
            });

            // Execute all handlers in parallel - exceptions are collected by Task.WhenAll into AggregateException
            var whenAllTask = Task.WhenAll(tasks);
            try
            {
                await whenAllTask.ConfigureAwait(false);
            }
            catch
            {
                // If multiple handlers throw, propagate the AggregateException with all exceptions
                if (whenAllTask.Exception != null && whenAllTask.Exception.InnerExceptions.Count > 1)
                {
                    throw whenAllTask.Exception;
                }
                throw;
            }
        }
    }
}
