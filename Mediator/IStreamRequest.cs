namespace Mediator;

/// <summary>
/// Marker interface for streaming requests.
/// </summary>
/// <typeparam name="TResponse">The type of items in the stream.</typeparam>
#pragma warning disable CA1040, S2326 // Avoid empty interfaces - marker interface by design
public interface IStreamRequest<out TResponse> { }
#pragma warning restore CA1040, S2326
