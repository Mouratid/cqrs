namespace Mediator;

/// <summary>
/// Represents a void type for requests that don't return a value.
/// </summary>
public readonly record struct Unit
{
    public static Unit Value { get; }
}
