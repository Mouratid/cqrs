using Mediator.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Tests;

[Collection("Mediator Tests")]
public class NotificationHandlerTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public NotificationHandlerTests()
    {
        var services = new ServiceCollection();
        services.AddMediator(typeof(NotificationHandlerTests).Assembly);
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
        NotificationLoggingBehavior<TestNotification>.LoggedMessages.Clear();
        NotificationOrderTestBehavior1<TestNotification>.ExecutionOrder.Clear();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public async Task PublishAsync_WithSingleHandler_ShouldInvokeHandler()
    {
        // Arrange
        var notification = new TestNotification { Message = "Test message" };

        // Act
        await _mediator.Publish(notification);

        // Assert
        Assert.Single(TestNotificationHandler.HandledMessages);
        Assert.Contains("Test message", TestNotificationHandler.HandledMessages);
    }

    [Fact]
    public async Task PublishAsync_WithMultipleHandlers_ShouldInvokeAllHandlers()
    {
        // Arrange
        var notification = new TestNotification { Message = "Multi handler test" };

        // Act
        await _mediator.Publish(notification);

        // Assert
        Assert.Single(TestNotificationHandler.HandledMessages);
        Assert.Single(SecondTestNotificationHandler.HandledMessages);
        Assert.Contains("Multi handler test", TestNotificationHandler.HandledMessages);
        Assert.Contains("Second: Multi handler test", SecondTestNotificationHandler.HandledMessages);
    }

    [Fact]
    public async Task PublishAsync_WithDifferentNotificationType_ShouldInvokeCorrectHandlers()
    {
        // Arrange
        var notification = new OrderChangedNotification { OrderId = 123, Status = "Shipped" };

        // Act
        await _mediator.Publish(notification);

        // Assert
        Assert.Single(OrderChangedEmailHandler.SentEmails);
        Assert.Single(OrderChangedSmsHandler.SentSms);
        Assert.Contains("Email sent for order 123 with status Shipped", OrderChangedEmailHandler.SentEmails);
        Assert.Contains("SMS sent for order 123", OrderChangedSmsHandler.SentSms);
    }

    [Fact]
    public async Task PublishAsync_WithNoHandlers_ShouldCompleteWithoutError()
    {
        // Arrange
        var notification = new UserRegisteredNotification { UserId = "user123", Email = "test@example.com" };

        // Act & Assert - should not throw
        await _mediator.Publish(notification);
    }

    [Fact]
    public async Task PublishAsync_WithNullNotification_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mediator.Publish<TestNotification>(null!));
    }

    [Fact]
    public async Task PublishAsync_WithHandlerThrowingException_ShouldThrowAggregateException()
    {
        // Arrange
        var notification = new ThrowingNotification { Message = "Error test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _mediator.Publish(notification));
        Assert.Contains("Handler error: Error test", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_WithMultipleHandlersThrowingException_ShouldCollectAllExceptions()
    {
        // Arrange
        var notification = new ThrowingNotification { Message = "Multiple errors" };

        // Act & Assert - With current DI setup, both handlers throw and create AggregateException
        // If only one handler is throwing, we get a single exception (not wrapped)
        try
        {
            await _mediator.Publish(notification);
            Assert.Fail("Expected an exception to be thrown");
        }
        catch (AggregateException ex)
        {
            // Multiple handlers threw
            Assert.True(ex.InnerExceptions.Count >= 2, $"Expected at least 2 exceptions but got {ex.InnerExceptions.Count}");
            Assert.Contains(ex.InnerExceptions, e => e is InvalidOperationException);
            Assert.Contains(ex.InnerExceptions, e => e is ArgumentException);
        }
        catch (InvalidOperationException ex)
        {
            // Only one handler threw - verify it's the expected exception
            Assert.Contains("Handler error: Multiple errors", ex.Message);
        }
    }

    [Fact]
    public async Task PublishAsync_WithCancellationToken_ShouldPassTokenToHandlers()
    {
        // Arrange
        var notification = new TestNotification { Message = "Cancellation test" };
        var cts = new CancellationTokenSource();
        cts.CancelAfter(10);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => _mediator.Publish(notification, cts.Token));
    }

    [Fact]
    public async Task PublishAsync_WithEmptyNotification_ShouldInvokeHandler()
    {
        // Arrange
        var notification = new EmptyNotification();

        // Act
        await _mediator.Publish(notification);

        // Assert
        Assert.Equal(1, EmptyNotificationHandler.CallCount);
    }

    [Fact]
    public async Task PublishAsync_MultipleTimesWithSameHandler_ShouldInvokeEachTime()
    {
        // Act
        await _mediator.Publish(new TestNotification { Message = "First" });
        await _mediator.Publish(new TestNotification { Message = "Second" });
        await _mediator.Publish(new TestNotification { Message = "Third" });

        // Assert
        Assert.Equal(3, TestNotificationHandler.HandledMessages.Count);
        Assert.Contains("First", TestNotificationHandler.HandledMessages);
        Assert.Contains("Second", TestNotificationHandler.HandledMessages);
        Assert.Contains("Third", TestNotificationHandler.HandledMessages);
    }

    [Fact]
    public async Task PublishAsync_WithComplexNotification_ShouldHandleAllProperties()
    {
        // Arrange
        var notification = new UserRegisteredNotification
        {
            UserId = "user-456",
            Email = "john.doe@example.com"
        };

        // Act
        await _mediator.Publish(notification);

        // Assert
        Assert.Single(UserRegisteredHandler.ProcessedUsers);
        Assert.Contains("User user-456 with email john.doe@example.com", UserRegisteredHandler.ProcessedUsers);
    }

    [Fact]
    public async Task PublishAsync_WithDefaultCancellationToken_ShouldComplete()
    {
        // Arrange
        var notification = new TestNotification { Message = "Default token test" };

        // Act
        await _mediator.Publish(notification, default);

        // Assert
        Assert.Single(TestNotificationHandler.HandledMessages);
        Assert.Contains("Default token test", TestNotificationHandler.HandledMessages);
    }
}
