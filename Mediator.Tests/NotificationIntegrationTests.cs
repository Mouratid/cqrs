using Mediator.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Tests;

[Collection("Mediator Tests")]
public class NotificationIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public NotificationIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddMediator(typeof(NotificationIntegrationTests).Assembly);
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();

        // Reset static state before each test
        TestNotificationHandler.HandledMessages.Clear();
        SecondTestNotificationHandler.HandledMessages.Clear();
        OrderChangedEmailHandler.SentEmails.Clear();
        OrderChangedSmsHandler.SentSms.Clear();
        UserRegisteredHandler.ProcessedUsers.Clear();
        DelayedNotificationHandler.HandledMessages.Clear();
        EmptyNotificationHandler.CallCount = 0;
        TestCommandHandler.LastValue = 0;
        NotificationLoggingBehavior<TestNotification>.LoggedMessages.Clear();
        NotificationOrderTestBehavior1<TestNotification>.ExecutionOrder.Clear();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task EndToEnd_NotificationWithMultipleHandlersAndBehaviors_ShouldWorkCorrectly()
    {
        // Arrange
        var notification = new TestNotification { Message = "Full integration test" };

        // Act
        await _mediator.Publish(notification);

        // Assert - All handlers executed
        Assert.Contains("Full integration test", TestNotificationHandler.HandledMessages);
        Assert.Contains("Second: Full integration test", SecondTestNotificationHandler.HandledMessages);

        // Assert - Behaviors executed for each handler
        Assert.True(NotificationLoggingBehavior<TestNotification>.LoggedMessages.Count >= 2);

        // Assert - Order maintained
        Assert.True(NotificationOrderTestBehavior1<TestNotification>.ExecutionOrder.Count >= 2);
    }

    [Fact]
    public async Task RealWorldScenario_OrderProcessing_ShouldNotifyAllStakeholders()
    {
        // Act - Simulate order status change
        await _mediator.Publish(new OrderChangedNotification
        {
            OrderId = 999,
            Status = "Processing"
        });

        await _mediator.Publish(new OrderChangedNotification
        {
            OrderId = 999,
            Status = "Shipped"
        });

        await _mediator.Publish(new OrderChangedNotification
        {
            OrderId = 999,
            Status = "Delivered"
        });

        // Assert
        Assert.Equal(3, OrderChangedEmailHandler.SentEmails.Count);
        Assert.Equal(3, OrderChangedSmsHandler.SentSms.Count);
        Assert.Contains("Email sent for order 999 with status Processing", OrderChangedEmailHandler.SentEmails);
        Assert.Contains("Email sent for order 999 with status Shipped", OrderChangedEmailHandler.SentEmails);
        Assert.Contains("Email sent for order 999 with status Delivered", OrderChangedEmailHandler.SentEmails);
    }

    [Fact]
    public async Task RealWorldScenario_UserRegistration_ShouldTriggerWelcomeWorkflow()
    {
        // Act
        await _mediator.Publish(new UserRegisteredNotification
        {
            UserId = "user-001",
            Email = "newuser@example.com"
        });

        // Assert
        Assert.Single(UserRegisteredHandler.ProcessedUsers);
        Assert.Contains("User user-001 with email newuser@example.com", UserRegisteredHandler.ProcessedUsers);
    }

    [Fact]
    public async Task MixedScenario_RequestsAndNotifications_ShouldWorkTogether()
    {
        // Act - Send a command
        await _mediator.SendAsync(new TestCommand { Value = 42 });

        // Act - Publish a notification
        await _mediator.Publish(new TestNotification { Message = "Command completed" });

        // Assert - Both should work independently
        Assert.Equal(42, TestCommandHandler.LastValue);
        Assert.Contains("Command completed", TestNotificationHandler.HandledMessages);
    }

    [Fact]
    public async Task ParallelNotifications_ShouldHandleCorrectly()
    {
        // Act - Publish multiple notifications in parallel
        await Task.WhenAll(
            _mediator.Publish(new TestNotification { Message = "Notification 1" }),
            _mediator.Publish(new TestNotification { Message = "Notification 2" }),
            _mediator.Publish(new OrderChangedNotification { OrderId = 1, Status = "Processing" }),
            _mediator.Publish(new UserRegisteredNotification { UserId = "user-1", Email = "test1@example.com" })
        );

        // Assert
        Assert.Equal(2, TestNotificationHandler.HandledMessages.Count);
        Assert.Single(OrderChangedEmailHandler.SentEmails);
        Assert.Single(UserRegisteredHandler.ProcessedUsers);
    }

    [Fact]
    public async Task SequentialNotifications_ShouldMaintainOrder()
    {
        // Act
        await _mediator.Publish(new TestNotification { Message = "First" });
        await _mediator.Publish(new TestNotification { Message = "Second" });
        await _mediator.Publish(new TestNotification { Message = "Third" });

        // Assert
        Assert.Equal(3, TestNotificationHandler.HandledMessages.Count);
        Assert.Equal("First", TestNotificationHandler.HandledMessages[0]);
        Assert.Equal("Second", TestNotificationHandler.HandledMessages[1]);
        Assert.Equal("Third", TestNotificationHandler.HandledMessages[2]);
    }

    [Fact]
    public async Task NotificationWithValidation_ValidInput_ShouldSucceed()
    {
        // Act
        await _mediator.Publish(new TestNotification { Message = "Valid notification" });

        // Assert
        Assert.Single(TestNotificationHandler.HandledMessages);
        Assert.Contains("Valid notification", TestNotificationHandler.HandledMessages);
    }

    [Fact]
    public async Task NotificationWithValidation_InvalidInput_ShouldThrow()
    {
        // Act & Assert - 3 handlers means 3 validation failures wrapped in AggregateException
        var exception = await Assert.ThrowsAsync<AggregateException>(() =>
            _mediator.Publish(new TestNotification { Message = "" }));
        Assert.Equal(3, exception.InnerExceptions.Count);
        Assert.All(exception.InnerExceptions, e => Assert.IsType<ArgumentException>(e));
        Assert.Empty(TestNotificationHandler.HandledMessages);
    }

    [Fact]
    public async Task ComplexWorkflow_MultipleNotificationsWithDifferentHandlers_ShouldComplete()
    {
        // Act - Simulate a complex business workflow
        // 1. User registers
        await _mediator.Publish(new UserRegisteredNotification
        {
            UserId = "user-complex",
            Email = "complex@example.com"
        });

        // 2. User places order
        await _mediator.Publish(new OrderChangedNotification
        {
            OrderId = 5000,
            Status = "Placed"
        });

        // 3. Order is processed
        await _mediator.Publish(new OrderChangedNotification
        {
            OrderId = 5000,
            Status = "Processing"
        });

        // 4. Order is shipped
        await _mediator.Publish(new OrderChangedNotification
        {
            OrderId = 5000,
            Status = "Shipped"
        });

        // 5. Send general notification
        await _mediator.Publish(new TestNotification
        {
            Message = "Order workflow completed"
        });

        // Assert
        Assert.Single(UserRegisteredHandler.ProcessedUsers);
        Assert.Equal(3, OrderChangedEmailHandler.SentEmails.Count);
        Assert.Equal(3, OrderChangedSmsHandler.SentSms.Count);
        Assert.Single(TestNotificationHandler.HandledMessages);
    }

    [Fact]
    public async Task NotificationWithMultipleBehaviors_ShouldApplyAllBehaviors()
    {
        // Arrange
        var notification = new TestNotification { Message = "Multiple behaviors test" };

        // Act
        await _mediator.Publish(notification);

        // Assert - Handler executed
        Assert.Contains("Multiple behaviors test", TestNotificationHandler.HandledMessages);

        // Assert - All behaviors executed
        Assert.NotEmpty(NotificationLoggingBehavior<TestNotification>.LoggedMessages);
        Assert.NotEmpty(NotificationOrderTestBehavior1<TestNotification>.ExecutionOrder);
    }

    [Fact]
    public async Task EmptyNotification_WithHandler_ShouldExecute()
    {
        // Act
        await _mediator.Publish(new EmptyNotification());
        await _mediator.Publish(new EmptyNotification());

        // Assert
        Assert.Equal(2, EmptyNotificationHandler.CallCount);
    }

    [Fact]
    public async Task NotificationHandlerWithDelay_ShouldCompleteSuccessfully()
    {
        // Arrange
        var notification = new TestNotification { Message = "Delayed handling" };

        // Act
        await _mediator.Publish(notification);

        // Assert
        Assert.Contains("Delayed: Delayed handling", DelayedNotificationHandler.HandledMessages);
    }

    [Fact]
    public async Task MultiplePublishCalls_ShouldMaintainIsolation()
    {
        // Act - Multiple publishes with different data
        await _mediator.Publish(new TestNotification { Message = "A" });
        await _mediator.Publish(new TestNotification { Message = "B" });
        await _mediator.Publish(new TestNotification { Message = "C" });

        // Assert - Each notification handled independently
        Assert.Equal(3, TestNotificationHandler.HandledMessages.Count);
        Assert.Equal(3, SecondTestNotificationHandler.HandledMessages.Count);
        Assert.Contains("A", TestNotificationHandler.HandledMessages);
        Assert.Contains("B", TestNotificationHandler.HandledMessages);
        Assert.Contains("C", TestNotificationHandler.HandledMessages);
    }
}
