using System.Diagnostics.CodeAnalysis;

namespace Mediator
{
    /// <summary>
    /// Represents a notification that can be published through the mediator.
    /// </summary>
    [SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Marker interface for notifications")]
    public interface INotification
    {
    }
}
