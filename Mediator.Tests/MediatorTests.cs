using FluentAssertions;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Tests;

public class MediatorTests
{
    [Fact]
    public async Task SendAsync_WithValidQuery_ShouldReturnResponse()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<TestQuery, TestResponse>, TestQueryHandler>();
        services.AddScoped<IMediator, Mediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var query = new TestQuery(42);

        // Act
        var result = await mediator.SendAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(42);
        result.Message.Should().Be("Success");
    }

    [Fact]
    public async Task SendAsync_WithValidCommand_ShouldReturnResponse()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<TestCommand, TestResponse>, TestCommandHandler>();
        services.AddScoped<IMediator, Mediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var command = new TestCommand("Test");

        // Act
        var result = await mediator.SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(100);
        result.Message.Should().Be("Test");
    }

    [Fact]
    public async Task SendAsync_WithCommandWithoutResponse_ShouldReturnUnit()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<TestCommandWithoutResponse, Unit>, TestCommandWithoutResponseHandler>();
        services.AddScoped<IMediator, Mediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var command = new TestCommandWithoutResponse();

        // Act
        var result = await mediator.SendAsync(command);

        // Assert
        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task SendAsync_WithUnregisteredHandler_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IMediator, Mediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var query = new TestQuery(42);

        // Act
        var act = () => mediator.SendAsync(query);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No handler registered for request type 'TestQuery'");
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IMediator, Mediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var act = () => mediator.SendAsync<TestResponse>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // Test fixtures
    public record TestQuery(int Value) : IRequest<TestResponse>;

    public record TestCommand(string Name) : IRequest<TestResponse>;

    public record TestCommandWithoutResponse : IRequest;

    public record TestResponse(int Value, string Message);

    public class TestQueryHandler : IRequestHandler<TestQuery, TestResponse>
    {
        public Task<TestResponse> HandleAsync(TestQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TestResponse(request.Value, "Success"));
        }
    }

    public class TestCommandHandler : IRequestHandler<TestCommand, TestResponse>
    {
        public Task<TestResponse> HandleAsync(TestCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TestResponse(100, request.Name));
        }
    }

    public class TestCommandWithoutResponseHandler : IRequestHandler<TestCommandWithoutResponse, Unit>
    {
        public Task<Unit> HandleAsync(TestCommandWithoutResponse request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }
    }
}
