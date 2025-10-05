using Microsoft.Extensions.DependencyInjection;

namespace Mediator;

internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);

        // Get the handler from DI
        var handler = serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for request type '{requestType.Name}'");

        // Get behaviors from DI
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);
        var behaviors = serviceProvider.GetServices(behaviorType).Reverse().ToList();

        // Build the pipeline
        RequestHandler<TResponse> handlerDelegate = async () =>
        {
            var handleMethod = handlerType.GetMethod(nameof(IRequestHandler<IRequest<object>, object>.HandleAsync))!;
            var result = handleMethod.Invoke(handler, [request, cancellationToken]);
            return await ((Task<TResponse>)result!).ConfigureAwait(false);
        };

        // Wrap handler with behaviors
        foreach (var behavior in behaviors)
        {
            var currentDelegate = handlerDelegate;
            var behaviorHandleMethod = behaviorType.GetMethod(nameof(IPipelineBehavior<IRequest<object>, object>.Handle))!;

            handlerDelegate = () =>
            {
                var result = behaviorHandleMethod.Invoke(behavior, [request, currentDelegate, cancellationToken]);
                return (Task<TResponse>)result!;
            };
        }

        return await handlerDelegate().ConfigureAwait(false);
    }
}
