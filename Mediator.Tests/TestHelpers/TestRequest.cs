namespace Mediator.Tests.TestHelpers;

public class TestQuery : IRequest<string>
{
    public string Input { get; set; } = string.Empty;
}

public class TestCommand : IRequest<Unit>
{
    public int Value { get; set; }
}

public class TestResponse
{
    public string Message { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class TestRequestWithResponse : IRequest<TestResponse>
{
    public string Input { get; set; } = string.Empty;
    public int Number { get; set; }
}

public class TestVoidCommand : IRequest
{
    public string Action { get; set; } = string.Empty;
}

public class ThrowingQuery : IRequest<string>
{
    public string Input { get; set; } = string.Empty;
}
