using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the mediator and scans the provided assemblies for request handlers and pipeline behaviors.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for handlers. If none provided, uses the calling assembly.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

        // Register mediator as scoped to align with typical handler lifetime
        services.AddScoped<IMediator, Mediator>();

        // If no assemblies provided, use the calling assembly
        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetCallingAssembly()];
        }

        // Scan for handlers
        var handlerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
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
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
                .Select(i => new { Interface = i, Implementation = t }))
            .ToList();

        foreach (var behavior in behaviorTypes)
        {
            services.AddScoped(behavior.Interface, behavior.Implementation);
        }

        return services;
    }
}
