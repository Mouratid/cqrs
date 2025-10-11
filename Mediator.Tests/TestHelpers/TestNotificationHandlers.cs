namespace Mediator.Tests.TestHelpers;

public class TestNotificationHandler : INotificationHandler<TestNotification>
{
    public static List<string> HandledMessages { get; } = new();

    public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        HandledMessages.Add(notification.Message);
        return Task.CompletedTask;
    }
}

public class SecondTestNotificationHandler : INotificationHandler<TestNotification>
{
    public static List<string> HandledMessages { get; } = new();

    public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        HandledMessages.Add($"Second: {notification.Message}");
        return Task.CompletedTask;
    }
}

public class OrderChangedEmailHandler : INotificationHandler<OrderChangedNotification>
{
    public static List<string> SentEmails { get; } = new();

    public Task HandleAsync(OrderChangedNotification notification, CancellationToken cancellationToken)
    {
        SentEmails.Add($"Email sent for order {notification.OrderId} with status {notification.Status}");
        return Task.CompletedTask;
    }
}

public class OrderChangedSmsHandler : INotificationHandler<OrderChangedNotification>
{
    public static List<string> SentSms { get; } = new();

    public Task HandleAsync(OrderChangedNotification notification, CancellationToken cancellationToken)
    {
        SentSms.Add($"SMS sent for order {notification.OrderId}");
        return Task.CompletedTask;
    }
}

public class UserRegisteredHandler : INotificationHandler<UserRegisteredNotification>
{
    public static List<string> ProcessedUsers { get; } = new();

    public Task HandleAsync(UserRegisteredNotification notification, CancellationToken cancellationToken)
    {
        ProcessedUsers.Add($"User {notification.UserId} with email {notification.Email}");
        return Task.CompletedTask;
    }
}

public class ThrowingNotificationHandler : INotificationHandler<ThrowingNotification>
{
    public Task HandleAsync(ThrowingNotification notification, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException($"Handler error: {notification.Message}");
    }
}

public class SecondThrowingNotificationHandler : INotificationHandler<ThrowingNotification>
{
    public Task HandleAsync(ThrowingNotification notification, CancellationToken cancellationToken)
    {
        throw new ArgumentException($"Second handler error: {notification.Message}");
    }
}

public class DelayedNotificationHandler : INotificationHandler<TestNotification>
{
    public static List<string> HandledMessages { get; } = new();

    public async Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken);
        HandledMessages.Add($"Delayed: {notification.Message}");
    }
}

public class EmptyNotificationHandler : INotificationHandler<EmptyNotification>
{
    public static int CallCount { get; set; }

    public Task HandleAsync(EmptyNotification notification, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.CompletedTask;
    }
}

// Abstract handler to test that abstract classes are ignored
public abstract class AbstractTestNotificationHandler : INotificationHandler<TestNotification>
{
    public abstract Task HandleAsync(TestNotification notification, CancellationToken cancellationToken);
}

// Generic type definition to test that open generics are ignored
public class GenericTestNotificationHandler<T> : INotificationHandler<TestNotification>
{
    public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

// Interface implementing INotificationHandler to test that interfaces are ignored
public interface ITestNotificationHandlerInterface : INotificationHandler<TestNotification>
{
}

// Struct implementing INotificationHandler to test that structs are ignored
public struct TestNotificationHandlerStruct : INotificationHandler<TestNotification>
{
    public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
