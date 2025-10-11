using Mediator.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddMediator_RegistersMediatorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(DependencyInjectionTests).Assembly);

        // Assert
        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IMediator));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
        Assert.Equal(typeof(Mediator), serviceDescriptor.ImplementationType);
    }

    [Fact]
    public void AddMediator_WithNoAssemblies_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => services.AddMediator());
        Assert.Equal("At least one assembly must be provided. (Parameter 'assemblies')", exception.Message);
        Assert.Equal("assemblies", exception.ParamName);
    }

    [Fact]
    public void AddMediator_WithSpecificAssemblies_ScansThoseAssemblies()
    {
        // Arrange
        var services = new ServiceCollection();
        var testAssembly = typeof(TestQueryHandler).Assembly;

        // Act
        services.AddMediator(testAssembly);

        // Assert
        using var serviceProvider = services.BuildServiceProvider();

        // Verify handlers are registered
        var queryHandler = serviceProvider.GetService<IRequestHandler<TestQuery, string>>();
        var commandHandler = serviceProvider.GetService<IRequestHandler<TestCommand, Unit>>();

        Assert.NotNull(queryHandler);
        Assert.NotNull(commandHandler);
        Assert.IsType<TestQueryHandler>(queryHandler);
        Assert.IsType<TestCommandHandler>(commandHandler);
    }

    [Fact]
    public void AddMediator_RegistersAllHandlersFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(DependencyInjectionTests).Assembly);

        // Assert
        using var serviceProvider = services.BuildServiceProvider();

        var queryHandler = serviceProvider.GetService<IRequestHandler<TestQuery, string>>();
        var commandHandler = serviceProvider.GetService<IRequestHandler<TestCommand, Unit>>();
        var responseHandler = serviceProvider.GetService<IRequestHandler<TestRequestWithResponse, TestResponse>>();
        var voidCommandHandler = serviceProvider.GetService<IRequestHandler<TestVoidCommand, Unit>>();

        Assert.NotNull(queryHandler);
        Assert.NotNull(commandHandler);
        Assert.NotNull(responseHandler);
        Assert.NotNull(voidCommandHandler);
    }

    [Fact]
    public void AddMediator_RegistersPipelineBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(DependencyInjectionTests).Assembly);

        // Assert
        using var serviceProvider = services.BuildServiceProvider();

        var loggingBehaviors = serviceProvider.GetServices<IPipelineBehavior<TestQuery, string>>();
        Assert.NotEmpty(loggingBehaviors);

        // Should contain concrete logging and validation behavior implementations
        var behaviorTypes = loggingBehaviors.Select(b => b.GetType()).ToList();
        Assert.Contains(typeof(TestQueryLoggingBehavior), behaviorTypes);
        Assert.Contains(typeof(TestQueryValidationBehavior), behaviorTypes);
    }

    [Fact]
    public void AddMediator_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DependencyInjection.AddMediator(null!, Array.Empty<Assembly>()));
    }

    [Fact]
    public void AddMediator_WithNullAssemblies_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddMediator(null!));
    }

    [Fact]
    public void AddMediator_RegistersHandlersAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(DependencyInjectionTests).Assembly);

        // Assert
        var handlerDescriptor = services.FirstOrDefault(s =>
            s.ServiceType == typeof(IRequestHandler<TestQuery, string>));

        Assert.NotNull(handlerDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, handlerDescriptor.Lifetime);
        Assert.Equal(typeof(TestQueryHandler), handlerDescriptor.ImplementationType);
    }

    [Fact]
    public void AddMediator_RegistersBehaviorsAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(DependencyInjectionTests).Assembly);

        // Assert
        var behaviorDescriptor = services.FirstOrDefault(s =>
            s.ServiceType == typeof(IPipelineBehavior<TestQuery, string>) &&
            s.ImplementationType == typeof(TestQueryLoggingBehavior));

        Assert.NotNull(behaviorDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, behaviorDescriptor.Lifetime);
    }

    [Fact]
    public void AddMediator_AllowsChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddMediator(typeof(DependencyInjectionTests).Assembly);

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddMediator_IgnoresAbstractClasses()
    {
        // This test verifies that abstract classes implementing IRequestHandler are not registered
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(DependencyInjectionTests).Assembly);

        // Assert
        using var serviceProvider = services.BuildServiceProvider();

        // All handlers should be concrete implementations
        var allHandlers = services.Where(s =>
            s.ServiceType.IsGenericType &&
            s.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

        foreach (var handler in allHandlers)
        {
            Assert.NotNull(handler.ImplementationType);
            Assert.False(handler.ImplementationType.IsAbstract);
            Assert.True(handler.ImplementationType.IsClass);
        }
    }

    [Fact]
    public void AddMediator_IgnoresGenericTypeDefinitions()
    {
        // This test verifies that open generic types are not registered
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(DependencyInjectionTests).Assembly);

        // Assert
        var allHandlers = services.Where(s =>
            s.ServiceType.IsGenericType &&
            s.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

        foreach (var handler in allHandlers)
        {
            Assert.NotNull(handler.ImplementationType);
            Assert.False(handler.ImplementationType.IsGenericTypeDefinition);
        }
    }

    [Fact]
    public void AddMediator_IgnoresInterfacesAndStructs()
    {
        // This test verifies that interfaces and structs implementing IRequestHandler are not registered
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(DependencyInjectionTests).Assembly);

        // Assert
        using var serviceProvider = services.BuildServiceProvider();

        // Verify no services are registered for interface or struct implementations
        var allHandlers = services.Where(s =>
            s.ServiceType.IsGenericType &&
            s.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

        foreach (var handler in allHandlers)
        {
            Assert.NotNull(handler.ImplementationType);
            Assert.True(handler.ImplementationType.IsClass);
            Assert.False(handler.ImplementationType.IsInterface);
            Assert.False(handler.ImplementationType.IsValueType); // Structs are value types
        }
    }

    [Fact]
    public void AddMediator_IgnoresBehaviorInterfacesAndStructs()
    {
        // This test verifies that interfaces and structs implementing IPipelineBehavior are not registered
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(DependencyInjectionTests).Assembly);

        // Assert
        var allBehaviors = services.Where(s =>
            s.ServiceType.IsGenericType &&
            s.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        foreach (var behavior in allBehaviors)
        {
            Assert.NotNull(behavior.ImplementationType);
            Assert.True(behavior.ImplementationType.IsClass);
            Assert.False(behavior.ImplementationType.IsInterface);
            Assert.False(behavior.ImplementationType.IsValueType); // Structs are value types
        }
    }

    [Fact]
    public void AddMediator_IgnoresAbstractBehaviors()
    {
        // This test verifies that abstract behaviors are not registered
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(DependencyInjectionTests).Assembly);

        // Assert
        var allBehaviors = services.Where(s =>
            s.ServiceType.IsGenericType &&
            s.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        foreach (var behavior in allBehaviors)
        {
            Assert.NotNull(behavior.ImplementationType);
            Assert.False(behavior.ImplementationType.IsAbstract);
            Assert.True(behavior.ImplementationType.IsClass);
        }
    }

    [Fact]
    public void AddMediator_IgnoresBehaviorGenericTypeDefinitions()
    {
        // This test verifies that open generic behavior types are not registered
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(DependencyInjectionTests).Assembly);

        // Assert
        var allBehaviors = services.Where(s =>
            s.ServiceType.IsGenericType &&
            s.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        foreach (var behavior in allBehaviors)
        {
            Assert.NotNull(behavior.ImplementationType);
            Assert.False(behavior.ImplementationType.IsGenericTypeDefinition);
        }
    }
}
