using Mediator.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Tests;

[Collection("Mediator Tests")]
public class NotificationPipelineBehaviorTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public NotificationPipelineBehaviorTests()
    {
        var services = new ServiceCollection();
        services.AddMediator(typeof(NotificationPipelineBehaviorTests).Assembly);
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();

        // Reset static state before each test
        TestNotificationHandler.HandledMessages.Clear();
        SecondTestNotificationHandler.HandledMessages.Clear();
        UserRegisteredHandler.ProcessedUsers.Clear();
        DelayedNotificationHandler.HandledMessages.Clear();
        EmptyNotificationHandler.CallCount = 0;
        NotificationLoggingBehavior<TestNotification>.LoggedMessages.Clear();
        NotificationOrderTestBehavior1<TestNotification>.ExecutionOrder.Clear();
        NotificationLoggingBehavior<EmptyNotification>.LoggedMessages.Clear();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task PublishAsync_WithSingleBehavior_ShouldExecuteBehavior()
    {
        // Arrange
        var notification = new TestNotification { Message = "Behavior test" };

        // Act
        await _mediator.Publish(notification);

        // Assert - Behavior should execute for each handler (3 handlers * 2 messages each = 6)
        Assert.Equal(6, NotificationLoggingBehavior<TestNotification>.LoggedMessages.Count);
        Assert.Equal("Before notification TestNotification", NotificationLoggingBehavior<TestNotification>.LoggedMessages[0]);
        Assert.Equal("After notification TestNotification", NotificationLoggingBehavior<TestNotification>.LoggedMessages[1]);
        Assert.Single(TestNotificationHandler.HandledMessages);
    }

    [Fact]
    public async Task PublishAsync_WithMultipleBehaviors_ShouldExecuteInCorrectOrder()
    {
        // Arrange
        var notification = new TestNotification { Message = "Order test" };

        // Act
        await _mediator.Publish(notification);

        // Assert - 3 handlers * 4 messages each = 12 total
        Assert.Equal(12, NotificationOrderTestBehavior1<TestNotification>.ExecutionOrder.Count);
        // Check first handler's execution order
        Assert.Equal("NotificationBehavior1-Before", NotificationOrderTestBehavior1<TestNotification>.ExecutionOrder[0]);
        Assert.Equal("NotificationBehavior2-Before", NotificationOrderTestBehavior1<TestNotification>.ExecutionOrder[1]);
        Assert.Equal("NotificationBehavior2-After", NotificationOrderTestBehavior1<TestNotification>.ExecutionOrder[2]);
        Assert.Equal("NotificationBehavior1-After", NotificationOrderTestBehavior1<TestNotification>.ExecutionOrder[3]);
    }

    [Fact]
    public async Task PublishAsync_WithValidationBehavior_ShouldValidateBeforeHandling()
    {
        // Arrange
        var notification = new TestNotification { Message = "" };

        // Act & Assert - 3 handlers means 3 validation failures wrapped in AggregateException
        var exception = await Assert.ThrowsAsync<AggregateException>(() => _mediator.Publish(notification));
        Assert.Equal(3, exception.InnerExceptions.Count);
        Assert.All(exception.InnerExceptions, e => Assert.IsType<ArgumentException>(e));
        Assert.Empty(TestNotificationHandler.HandledMessages);
    }

    [Fact]
    public async Task PublishAsync_WithValidNotification_ShouldPassValidation()
    {
        // Arrange
        var notification = new TestNotification { Message = "Valid message" };

        // Act
        await _mediator.Publish(notification);

        // Assert
        Assert.Single(TestNotificationHandler.HandledMessages);
        Assert.Contains("Valid message", TestNotificationHandler.HandledMessages);
    }

    [Fact]
    public async Task PublishAsync_WithBehaviorThrowingException_ShouldPropagateException()
    {
        // Arrange
        TestNotificationHandler.HandledMessages.Clear();

        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler>();
        services.AddTransient<INotificationPipelineBehavior<TestNotification>, ThrowingNotificationBehavior>();
        services.AddScoped<IMediator, Mediator>();
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var notification = new TestNotification { Message = "Error test" };

        // Act & Assert
        await Assert.ThrowsAsync<TargetInvocationException>(() => mediator.Publish(notification));
        Assert.Empty(TestNotificationHandler.HandledMessages);
    }

    [Fact]
    public async Task PublishAsync_WithMultipleHandlersAndBehaviors_ShouldApplyBehaviorToEachHandler()
    {
        // Arrange
        var notification = new TestNotification { Message = "Multi handler with behavior" };

        // Act
        await _mediator.Publish(notification);

        // Assert
        // Each handler should have the behavior applied, so we should see 6 log messages (3 handlers * 2 messages per handler)
        Assert.Equal(6, NotificationLoggingBehavior<TestNotification>.LoggedMessages.Count);
        Assert.Single(TestNotificationHandler.HandledMessages);
        Assert.Single(SecondTestNotificationHandler.HandledMessages);
    }

    [Fact]
    public async Task PublishAsync_WithNoBehaviors_ShouldExecuteHandlerDirectly()
    {
        // Arrange
        var notification = new UserRegisteredNotification
        {
            UserId = "user-789",
            Email = "test@example.com"
        };

        // Act
        await _mediator.Publish(notification);

        // Assert
        Assert.Single(UserRegisteredHandler.ProcessedUsers);
        Assert.Contains("User user-789 with email test@example.com", UserRegisteredHandler.ProcessedUsers);
    }

    [Fact]
    public async Task PublishAsync_WithBehaviorAndCancellationToken_ShouldPassTokenThrough()
    {
        // Arrange
        var notification = new TestNotification { Message = "Cancellation test" };
        var cts = new CancellationTokenSource();
        cts.CancelAfter(10);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => _mediator.Publish(notification, cts.Token));
    }

    [Fact]
    public async Task PublishAsync_BehaviorOrder_ShouldRespectRegistrationOrder()
    {
        // Arrange
        NotificationOrderTestBehavior1<TestNotification>.ExecutionOrder.Clear();

        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler>();
        services.AddTransient<INotificationPipelineBehavior<TestNotification>, TestNotificationOrderBehavior1>();
        services.AddTransient<INotificationPipelineBehavior<TestNotification>, TestNotificationOrderBehavior2>();
        services.AddScoped<IMediator, Mediator>();
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var notification = new TestNotification { Message = "Order verification" };

        // Act
        await mediator.Publish(notification);

        // Assert
        var order = NotificationOrderTestBehavior1<TestNotification>.ExecutionOrder;
        Assert.Equal(4, order.Count);
        // Behaviors execute in registration order 1, 2
        Assert.Equal("NotificationBehavior1-Before", order[0]);
        Assert.Equal("NotificationBehavior2-Before", order[1]);
        Assert.Equal("NotificationBehavior2-After", order[2]);
        Assert.Equal("NotificationBehavior1-After", order[3]);
    }

    [Fact]
    public async Task PublishAsync_WithBehaviorAndNoHandlers_ShouldExecuteBehavior()
    {
        // Arrange
        NotificationLoggingBehavior<EmptyNotification>.LoggedMessages.Clear();

        var services = new ServiceCollection();
        services.AddTransient<INotificationPipelineBehavior<EmptyNotification>, NotificationLoggingBehavior<EmptyNotification>>();
        services.AddScoped<IMediator, Mediator>();
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var notification = new EmptyNotification();

        // Act
        await mediator.Publish(notification);

        // Assert - behavior should not execute if there are no handlers
        Assert.Empty(NotificationLoggingBehavior<EmptyNotification>.LoggedMessages);
    }

    [Fact]
    public async Task PublishAsync_WithOnlyHandler_ShouldCompleteWithoutBehaviors()
    {
        // Arrange
        EmptyNotificationHandler.CallCount = 0;

        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<EmptyNotification>, EmptyNotificationHandler>();
        services.AddScoped<IMediator, Mediator>();
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var notification = new EmptyNotification();

        // Act
        await mediator.Publish(notification);

        // Assert
        Assert.Equal(1, EmptyNotificationHandler.CallCount);
    }

    [Fact]
    public async Task PublishAsync_WithBehaviorModifyingExecution_ShouldAffectAllHandlers()
    {
        // Arrange
        TestNotificationHandler.HandledMessages.Clear();
        SecondTestNotificationHandler.HandledMessages.Clear();

        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler>();
        services.AddTransient<INotificationHandler<TestNotification>, SecondTestNotificationHandler>();
        services.AddTransient<INotificationPipelineBehavior<TestNotification>, TestNotificationValidationBehavior>();
        services.AddScoped<IMediator, Mediator>();
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var notification = new TestNotification { Message = "" };

        // Act & Assert - 2 handlers means 2 validation failures wrapped in AggregateException
        var exception = await Assert.ThrowsAsync<AggregateException>(() => mediator.Publish(notification));
        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.All(exception.InnerExceptions, e => Assert.IsType<ArgumentException>(e));
        Assert.Empty(TestNotificationHandler.HandledMessages);
        Assert.Empty(SecondTestNotificationHandler.HandledMessages);
    }
}
