namespace Mediator.Tests.TestHelpers;

public class TestNotification : INotification
{
    public string Message { get; set; } = string.Empty;
}

public class OrderChangedNotification : INotification
{
    public int OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class UserRegisteredNotification : INotification
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ThrowingNotification : INotification
{
    public string Message { get; set; } = string.Empty;
}

public class EmptyNotification : INotification
{
}
