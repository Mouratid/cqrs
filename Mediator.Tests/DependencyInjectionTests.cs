using System.Reflection;
using FluentAssertions;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddMediator_ShouldRegisterMediatorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMediator));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be(typeof(global::Mediator.Mediator));
    }

    [Fact]
    public void AddMediator_WithoutAssemblies_ShouldUseCallingAssembly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - IMediator should be registered
        var mediator = serviceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddMediator_WithAssembly_ShouldRegisterHandlersFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddMediator(assembly);

        // Assert - Handlers should be registered
        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IRequestHandler<SampleQuery, SampleResponse>));
        handlerDescriptor.Should().NotBeNull();
        handlerDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddMediator_WithAssembly_ShouldRegisterBehaviorsFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddMediator(assembly);

        // Assert - Behaviors should be registered
        var behaviorDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<SampleQuery, SampleResponse>));
        behaviorDescriptor.Should().NotBeNull();
        behaviorDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddMediator_ShouldNotRegisterGenericTypeDefinitions()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddMediator(assembly);

        // Assert - Generic type definitions should not be registered
        var genericHandlerDescriptor = services.FirstOrDefault(d =>
            d.ImplementationType?.IsGenericTypeDefinition == true);
        genericHandlerDescriptor.Should().BeNull();
    }

    [Fact]
    public async Task AddMediator_IntegrationTest_ShouldWorkEndToEnd()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var query = new SampleQuery("Test");

        // Act
        var result = await mediator.SendAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().Be("Test");
        result.WasHandled.Should().BeTrue();
    }

    [Fact]
    public void AddMediator_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddMediator();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMediator_WithNullAssemblies_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Assembly[] assemblies = null!;

        // Act
        var act = () => services.AddMediator(assemblies);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // Test fixtures
    public record SampleQuery(string Data) : IRequest<SampleResponse>;

    public record SampleResponse(string Data, bool WasHandled);

    public class SampleQueryHandler : IRequestHandler<SampleQuery, SampleResponse>
    {
        public Task<SampleResponse> HandleAsync(SampleQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SampleResponse(request.Data, true));
        }
    }

    public class SampleBehavior : IPipelineBehavior<SampleQuery, SampleResponse>
    {
        public async Task<SampleResponse> Handle(SampleQuery request, RequestHandler<SampleResponse> nextHandler, CancellationToken cancellationToken)
        {
            return await nextHandler();
        }
    }
}
