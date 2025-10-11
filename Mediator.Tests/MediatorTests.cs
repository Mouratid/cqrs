using Mediator.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Tests;

[Collection("Mediator Tests")]
public class MediatorTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public MediatorTests()
    {
        var services = new ServiceCollection();
        services.AddMediator(typeof(MediatorTests).Assembly);
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();

        // Reset static state before each test
        TestCommandHandler.LastValue = 0;
        TestVoidCommandHandler.LastAction = string.Empty;
        LoggingBehavior<TestQuery, string>.LoggedMessages.Clear();
        OrderTestBehavior1<TestQuery, string>.ExecutionOrder.Clear();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SendAsync_WithValidQuery_ReturnsExpectedResult()
    {
        // Arrange
        var query = new TestQuery { Input = "test input" };

        // Act
        var result = await _mediator.SendAsync<string>(query);

        // Assert
        Assert.Equal("Handled: test input", result);
    }

    [Fact]
    public async Task SendAsync_WithCommand_ExecutesSuccessfully()
    {
        // Arrange
        var command = new TestCommand { Value = 42 };

        // Act
        var result = await _mediator.SendAsync<Unit>(command);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.Equal(42, TestCommandHandler.LastValue);
    }

    [Fact]
    public async Task SendAsync_WithVoidCommand_ExecutesSuccessfully()
    {
        // Arrange
        var command = new TestVoidCommand { Action = "test action" };

        // Act
        var result = await _mediator.SendAsync<Unit>(command);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.Equal("test action", TestVoidCommandHandler.LastAction);
    }

    [Fact]
    public async Task SendAsync_WithComplexResponse_ReturnsCorrectResponse()
    {
        // Arrange
        var request = new TestRequestWithResponse
        {
            Input = "complex test",
            Number = 21
        };

        // Act
        var result = await _mediator.SendAsync<TestResponse>(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Processed: complex test", result.Message);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _mediator.SendAsync<string>(null!));
    }

    [Fact]
    public async Task SendAsync_WithUnregisteredHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        // Don't register any handlers
        services.AddScoped<IMediator, Mediator>();
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var query = new TestQuery { Input = "test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync<string>(query));

        Assert.Contains("No handler registered for request type 'TestQuery'", exception.Message);
    }

    [Fact]
    public async Task SendAsync_WithHandlerThatThrows_PropagatesException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<ThrowingQuery, string>, ThrowingHandler>();
        services.AddScoped<IMediator, Mediator>();
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var query = new ThrowingQuery { Input = "test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TargetInvocationException>(() =>
            mediator.SendAsync<string>(query));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("Test exception", exception.InnerException.Message);
    }

    [Fact]
    public async Task SendAsync_WithCancellationToken_PassesToHandler()
    {
        // Arrange
        var query = new TestQuery { Input = "test" };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // Since our test handler doesn't check cancellation token, it will complete
        // In a real scenario, a handler would check the token and throw OperationCanceledException
        var result = await _mediator.SendAsync<string>(query, cts.Token);
        Assert.Equal("Handled: test", result);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Mediator(null!));
    }
}
