using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator
{

    public static class DependencyInjection
    {
        /// <summary>
        /// Registers the mediator and scans the provided assemblies for request handlers, notification handlers, and pipeline behaviors.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">Assemblies to scan for handlers. At least one assembly must be provided.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services or assemblies is null.</exception>
        /// <exception cref="ArgumentException">Thrown when no assemblies are provided.</exception>
        public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));
            if (assemblies.Length == 0) throw new ArgumentException("At least one assembly must be provided.", nameof(assemblies));

            // Register mediator as scoped to align with typical handler lifetime
            services.AddScoped<IMediator, Mediator>();

            // Scan for handlers
            var handlerTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition && !t.IsValueType && t.IsPublic)
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                    .Select(i => new { Interface = i, Implementation = t }))
                .ToList();

            foreach (var handler in handlerTypes)
            {
                services.AddScoped(handler.Interface, handler.Implementation);
            }

            // Scan for pipeline behaviors
            var behaviorTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition && !t.IsValueType && t.IsPublic)
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
                    .Select(i => new { Interface = i, Implementation = t }))
                .ToList();

            foreach (var behavior in behaviorTypes)
            {
                services.AddScoped(behavior.Interface, behavior.Implementation);
            }

            // Scan for notification handlers
            var notificationHandlerTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition && !t.IsValueType && t.IsPublic)
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                    .Select(i => new { Interface = i, Implementation = t }))
                .ToList();

            foreach (var notificationHandler in notificationHandlerTypes)
            {
                services.AddScoped(notificationHandler.Interface, notificationHandler.Implementation);
            }

            // Scan for notification pipeline behaviors
            var notificationBehaviorTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition && !t.IsValueType && t.IsPublic)
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationPipelineBehavior<>))
                    .Select(i => new { Interface = i, Implementation = t }))
                .ToList();

            foreach (var notificationBehavior in notificationBehaviorTypes)
            {
                services.AddScoped(notificationBehavior.Interface, notificationBehavior.Implementation);
            }

            return services;
        }
    }
}
