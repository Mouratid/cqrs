namespace Mediator.Tests.TestHelpers;

public class NotificationLoggingBehavior<TNotification> : INotificationPipelineBehavior<TNotification>
    where TNotification : INotification
{
    public static List<string> LoggedMessages { get; } = new();

    public async Task HandleAsync(TNotification notification, NotificationHandler nextHandler, CancellationToken cancellationToken)
    {
        LoggedMessages.Add($"Before notification {typeof(TNotification).Name}");
        await nextHandler();
        LoggedMessages.Add($"After notification {typeof(TNotification).Name}");
    }
}

// Concrete implementation for specific notification type
public class TestNotificationLoggingBehavior : INotificationPipelineBehavior<TestNotification>
{
    public async Task HandleAsync(TestNotification notification, NotificationHandler nextHandler, CancellationToken cancellationToken)
    {
        NotificationLoggingBehavior<TestNotification>.LoggedMessages.Add($"Before notification {typeof(TestNotification).Name}");
        await nextHandler();
        NotificationLoggingBehavior<TestNotification>.LoggedMessages.Add($"After notification {typeof(TestNotification).Name}");
    }
}

public class NotificationValidationBehavior<TNotification> : INotificationPipelineBehavior<TNotification>
    where TNotification : INotification
{
    public async Task HandleAsync(TNotification notification, NotificationHandler nextHandler, CancellationToken cancellationToken)
    {
        if (notification is TestNotification testNotification && string.IsNullOrEmpty(testNotification.Message))
        {
            throw new ArgumentException("Notification message cannot be empty");
        }

        await nextHandler();
    }
}

public class TestNotificationValidationBehavior : INotificationPipelineBehavior<TestNotification>
{
    public async Task HandleAsync(TestNotification notification, NotificationHandler nextHandler, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(notification.Message))
        {
            throw new ArgumentException("Notification message cannot be empty");
        }

        await nextHandler();
    }
}

public class NotificationOrderTestBehavior1<TNotification> : INotificationPipelineBehavior<TNotification>
    where TNotification : INotification
{
    public static List<string> ExecutionOrder { get; } = new();

    public async Task HandleAsync(TNotification notification, NotificationHandler nextHandler, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("NotificationBehavior1-Before");
        await nextHandler();
        ExecutionOrder.Add("NotificationBehavior1-After");
    }
}

public class NotificationOrderTestBehavior2<TNotification> : INotificationPipelineBehavior<TNotification>
    where TNotification : INotification
{
    public async Task HandleAsync(TNotification notification, NotificationHandler nextHandler, CancellationToken cancellationToken)
    {
        NotificationOrderTestBehavior1<TNotification>.ExecutionOrder.Add("NotificationBehavior2-Before");
        await nextHandler();
        NotificationOrderTestBehavior1<TNotification>.ExecutionOrder.Add("NotificationBehavior2-After");
    }
}

// Concrete implementations for order testing
public class TestNotificationOrderBehavior1 : INotificationPipelineBehavior<TestNotification>
{
    public async Task HandleAsync(TestNotification notification, NotificationHandler nextHandler, CancellationToken cancellationToken)
    {
        NotificationOrderTestBehavior1<TestNotification>.ExecutionOrder.Add("NotificationBehavior1-Before");
        await nextHandler();
        NotificationOrderTestBehavior1<TestNotification>.ExecutionOrder.Add("NotificationBehavior1-After");
    }
}

public class TestNotificationOrderBehavior2 : INotificationPipelineBehavior<TestNotification>
{
    public async Task HandleAsync(TestNotification notification, NotificationHandler nextHandler, CancellationToken cancellationToken)
    {
        NotificationOrderTestBehavior1<TestNotification>.ExecutionOrder.Add("NotificationBehavior2-Before");
        await nextHandler();
        NotificationOrderTestBehavior1<TestNotification>.ExecutionOrder.Add("NotificationBehavior2-After");
    }
}

// Note: ThrowingNotificationBehavior should NOT be automatically registered via assembly scanning
// It's designed to be manually registered in specific tests only
// To prevent auto-registration, it's internal
internal class ThrowingNotificationBehavior : INotificationPipelineBehavior<TestNotification>
{
    public Task HandleAsync(TestNotification notification, NotificationHandler nextHandler, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Behavior error");
    }
}

// Abstract behavior to test that abstract classes are ignored
public abstract class AbstractTestNotificationBehavior : INotificationPipelineBehavior<TestNotification>
{
    public abstract Task HandleAsync(TestNotification notification, NotificationHandler nextHandler, CancellationToken cancellationToken);
}

// Generic type definition to test that open generics are ignored
public class GenericTestNotificationBehavior<T> : INotificationPipelineBehavior<TestNotification>
{
    public Task HandleAsync(TestNotification notification, NotificationHandler nextHandler, CancellationToken cancellationToken)
    {
        return nextHandler();
    }
}

// Interface implementing INotificationPipelineBehavior to test that interfaces are ignored
public interface ITestNotificationBehaviorInterface : INotificationPipelineBehavior<TestNotification>
{
}

// Struct implementing INotificationPipelineBehavior to test that structs are ignored
public struct TestNotificationBehaviorStruct : INotificationPipelineBehavior<TestNotification>
{
    public Task HandleAsync(TestNotification notification, NotificationHandler nextHandler, CancellationToken cancellationToken)
    {
        return nextHandler();
    }
}
