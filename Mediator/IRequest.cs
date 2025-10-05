namespace Mediator;

/// <summary>
/// Marker interface for requests without a response.
/// </summary>
#pragma warning disable CA1040 // Avoid empty interfaces - marker interface by design
public interface IRequest : IRequest<Unit> { }
#pragma warning restore CA1040

/// <summary>
/// Marker interface for requests with a response.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
#pragma warning disable CA1040, S2326 // Avoid empty interfaces - marker interface by design
public interface IRequest<out TResponse> { }
#pragma warning restore CA1040, S2326
