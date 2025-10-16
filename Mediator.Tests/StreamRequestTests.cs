using Mediator.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Tests;

public class StreamRequestTests
{
    [Fact]
    public async Task SendStreamAsync_WithValidRequest_StreamsResults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(typeof(TestStreamRequestHandler).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var results = new List<int>();
        await foreach (var item in mediator.SendStreamAsync(new TestStreamRequest(5)))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, results);
    }

    [Fact]
    public async Task SendStreamAsync_WithEmptyStream_ReturnsNoItems()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(typeof(EmptyStreamRequestHandler).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var results = new List<int>();
        await foreach (var item in mediator.SendStreamAsync(new EmptyStreamRequest()))
        {
            results.Add(item);
        }

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SendStreamAsync_WithCancellation_StopsStreaming()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(typeof(TestStreamRequestHandler).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var cts = new CancellationTokenSource();

        // Act
        var results = new List<int>();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in mediator.SendStreamAsync(new TestStreamRequest(100), cts.Token))
            {
                results.Add(item);
                if (item == 3)
                {
                    cts.Cancel();
                }
            }
        });

        // Assert
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task SendStreamAsync_WhenHandlerThrows_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(typeof(StreamRequestThrowsExceptionHandler).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        var results = new List<int>();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var item in mediator.SendStreamAsync(new StreamRequestThrowsException()))
            {
                results.Add(item);
            }
        });

        Assert.Equal("Stream error", exception.Message);
        Assert.Single(results); // Should have received first item before exception
    }

    [Fact]
    public async Task SendStreamAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(typeof(TestStreamRequestHandler).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await foreach (var item in mediator.SendStreamAsync<int>(null!))
            {
                // Should not reach here
            }
        });
    }

    [Fact]
    public async Task SendStreamAsync_WithNoHandlerRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(typeof(TestStreamRequestHandler).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var item in mediator.SendStreamAsync(new UnregisteredStreamRequest()))
            {
                // Should not reach here
            }
        });

        Assert.Contains("No stream handler registered", exception.Message);
    }

    [Fact]
    public async Task SendStreamAsync_MultipleEnumerations_ExecutesHandlerMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(typeof(TestStreamRequestHandler).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var request = new TestStreamRequest(3);

        // Act
        var results1 = new List<int>();
        await foreach (var item in mediator.SendStreamAsync(request))
        {
            results1.Add(item);
        }

        var results2 = new List<int>();
        await foreach (var item in mediator.SendStreamAsync(request))
        {
            results2.Add(item);
        }

        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, results1);
        Assert.Equal(new[] { 1, 2, 3 }, results2);
    }
}

public record UnregisteredStreamRequest : IStreamRequest<int>;
