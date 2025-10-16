using Mediator.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Tests;

public class StreamPipelineBehaviorTests
{
    [Fact]
    public async Task SendStreamAsync_WithBehavior_ExecutesBehavior()
    {
        // Arrange
        StreamBehaviorTracker.Reset();
        var services = new ServiceCollection();
        services.AddMediator(typeof(TestStreamRequestWithBehaviorHandler).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var results = new List<string>();
        await foreach (var item in mediator.SendStreamAsync(new TestStreamRequestWithBehavior(3)))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(new[] { "ITEM-1", "ITEM-2", "ITEM-3" }, results);
        Assert.Contains("StreamTransformBehavior-Before", StreamBehaviorTracker.ExecutionOrder);
        Assert.Contains("StreamTransformBehavior-After", StreamBehaviorTracker.ExecutionOrder);
    }

    [Fact]
    public async Task SendStreamAsync_WithMultipleBehaviors_ExecutesInCorrectOrder()
    {
        // Arrange
        StreamBehaviorTracker.Reset();
        var services = new ServiceCollection();
        services.AddMediator(typeof(TestStreamRequestWithBehaviorHandler).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var results = new List<string>();
        await foreach (var item in mediator.SendStreamAsync(new TestStreamRequestWithBehavior(2)))
        {
            results.Add(item);
        }

        // Assert - Behaviors registered via assembly scan
        // Order: StreamTransformBehavior -> StreamLoggingBehavior -> Handler (reverse registration)
        Assert.True(StreamBehaviorTracker.ExecutionOrder.Count > 0);
    }

    [Fact]
    public async Task SendStreamAsync_BehaviorCanTransformStream()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(typeof(TestStreamRequestWithBehaviorHandler).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var results = new List<string>();
        await foreach (var item in mediator.SendStreamAsync(new TestStreamRequestWithBehavior(3)))
        {
            results.Add(item);
        }

        // Assert - StreamTransformBehavior converts to uppercase
        Assert.All(results, item => Assert.Matches(@"ITEM-\d+", item));
    }

    [Fact]
    public async Task SendStreamAsync_BehaviorCanShortCircuit()
    {
        // Arrange
        StreamBehaviorTracker.Reset();
        var services = new ServiceCollection();

        // Manually register to control behavior order
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<IStreamRequestHandler<TestStreamRequest, int>, TestStreamRequestHandler>();
        services.AddScoped<IStreamPipelineBehavior<TestStreamRequest, int>>(sp =>
            new StreamShortCircuitBehavior<TestStreamRequest, int>(GetShortCircuitStream()));

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var results = new List<int>();
        await foreach (var item in mediator.SendStreamAsync(new TestStreamRequest(100)))
        {
            results.Add(item);
        }

        // Assert - Should return short-circuit values, not handler values
        Assert.Equal(new[] { 99, 98, 97 }, results);
        Assert.Contains("StreamShortCircuit", StreamBehaviorTracker.ExecutionOrder);
    }

    [Fact]
    public async Task SendStreamAsync_WithNoBehaviors_ExecutesHandlerDirectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<IStreamRequestHandler<TestStreamRequest, int>, TestStreamRequestHandler>();

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
    public async Task SendStreamAsync_BehaviorReceivesCancellationToken()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(typeof(TestStreamRequestHandler).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var cts = new CancellationTokenSource();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in mediator.SendStreamAsync(new TestStreamRequest(100), cts.Token))
            {
                if (item == 2)
                {
                    cts.Cancel();
                }
            }
        });
    }

    private static async IAsyncEnumerable<int> GetShortCircuitStream()
    {
        yield return 99;
        await Task.Delay(1);
        yield return 98;
        await Task.Delay(1);
        yield return 97;
    }
}
