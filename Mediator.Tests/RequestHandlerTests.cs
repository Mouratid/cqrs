using Mediator.Tests.TestHelpers;

namespace Mediator.Tests;

public class RequestHandlerTests
{
    [Fact]
    public async Task TestQueryHandler_WithValidInput_ReturnsFormattedString()
    {
        // Arrange
        var handler = new TestQueryHandler();
        var request = new TestQuery { Input = "hello world" };

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Handled: hello world", result);
    }

    [Fact]
    public async Task TestQueryHandler_WithEmptyInput_ReturnsFormattedString()
    {
        // Arrange
        var handler = new TestQueryHandler();
        var request = new TestQuery { Input = string.Empty };

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Handled: ", result);
    }

    [Fact]
    public async Task TestCommandHandler_ExecutesAndSetsStaticValue()
    {
        // Arrange
        var handler = new TestCommandHandler();
        var request = new TestCommand { Value = 123 };
        TestCommandHandler.LastValue = 0; // Reset

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.Equal(123, TestCommandHandler.LastValue);
    }

    [Fact]
    public async Task TestResponseHandler_WithValidInput_ReturnsComplexResponse()
    {
        // Arrange
        var handler = new TestResponseHandler();
        var request = new TestRequestWithResponse
        {
            Input = "test message",
            Number = 15
        };

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Processed: test message", result.Message);
        Assert.Equal(30, result.Value); // 15 * 2
    }

    [Fact]
    public async Task TestVoidCommandHandler_ExecutesAndSetsAction()
    {
        // Arrange
        var handler = new TestVoidCommandHandler();
        var request = new TestVoidCommand { Action = "delete user" };
        TestVoidCommandHandler.LastAction = string.Empty; // Reset

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.Equal("delete user", TestVoidCommandHandler.LastAction);
    }

    [Fact]
    public async Task ThrowingHandler_ThrowsExpectedException()
    {
        // Arrange
        var handler = new ThrowingHandler();
        var request = new ThrowingQuery { Input = "test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(request, CancellationToken.None));

        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public async Task TestHandlers_SupportCancellationToken()
    {
        // Arrange
        var handler = new TestQueryHandler();
        var request = new TestQuery { Input = "test" };
        using var cts = new CancellationTokenSource();

        // Act
        var result = await handler.HandleAsync(request, cts.Token);

        // Assert
        Assert.Equal("Handled: test", result);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 10)]
    [InlineData(-3, -6)]
    [InlineData(100, 200)]
    public async Task TestResponseHandler_WithDifferentNumbers_ReturnsDoubledValue(int input, int expected)
    {
        // Arrange
        var handler = new TestResponseHandler();
        var request = new TestRequestWithResponse
        {
            Input = "test",
            Number = input
        };

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("test")]
    [InlineData("special characters: !@#$%^&*()")]
    [InlineData("unicode: 🚀🌟")]
    public async Task TestQueryHandler_WithVariousInputs_ReturnsFormattedString(string input)
    {
        // Arrange
        var handler = new TestQueryHandler();
        var request = new TestQuery { Input = input };

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal($"Handled: {input}", result);
    }

    [Fact]
    public async Task MultipleHandlers_CanExecuteConcurrently()
    {
        // Arrange
        var queryHandler = new TestQueryHandler();
        var commandHandler = new TestCommandHandler();

        var query = new TestQuery { Input = "concurrent test" };
        var command = new TestCommand { Value = 999 };

        TestCommandHandler.LastValue = 0; // Reset

        // Act
        var queryTask = queryHandler.HandleAsync(query, CancellationToken.None);
        var commandTask = commandHandler.HandleAsync(command, CancellationToken.None);

        await Task.WhenAll(queryTask, commandTask);

        // Assert
        Assert.Equal("Handled: concurrent test", queryTask.Result);
        Assert.Equal(Unit.Value, commandTask.Result);
        Assert.Equal(999, TestCommandHandler.LastValue);
    }
}
