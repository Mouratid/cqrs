namespace Mediator.Tests.TestHelpers;

public record TestStreamRequest(int Count) : IStreamRequest<int>;

public record TestStreamRequestWithBehavior(int Count) : IStreamRequest<string>;

public record EmptyStreamRequest : IStreamRequest<int>;

public record StreamRequestThrowsException : IStreamRequest<int>;
