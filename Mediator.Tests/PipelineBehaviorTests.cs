using FluentAssertions;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Tests;

public class PipelineBehaviorTests
{
    [Fact]
    public async Task Pipeline_WithSingleBehavior_ShouldExecuteInOrder()
    {
        // Arrange
        var executionLog = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionLog);
        services.AddScoped<IRequestHandler<LoggingQuery, LoggingResponse>, LoggingQueryHandler>();
        services.AddScoped<IPipelineBehavior<LoggingQuery, LoggingResponse>, LoggingBehavior1>();
        services.AddScoped<IMediator, global::Mediator.Mediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new LoggingQuery());

        // Assert
        result.Should().NotBeNull();
        executionLog.Should().ContainInOrder(
            "Behavior1-Before",
            "Handler",
            "Behavior1-After"
        );
    }

    [Fact]
    public async Task Pipeline_WithMultipleBehaviors_ShouldExecuteInReverseRegistrationOrder()
    {
        // Arrange
        var executionLog = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionLog);
        services.AddScoped<IRequestHandler<LoggingQuery, LoggingResponse>, LoggingQueryHandler>();
        services.AddScoped<IPipelineBehavior<LoggingQuery, LoggingResponse>, LoggingBehavior1>();
        services.AddScoped<IPipelineBehavior<LoggingQuery, LoggingResponse>, LoggingBehavior2>();
        services.AddScoped<IPipelineBehavior<LoggingQuery, LoggingResponse>, LoggingBehavior3>();
        services.AddScoped<IMediator, global::Mediator.Mediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new LoggingQuery());

        // Assert
        result.Should().NotBeNull();
        executionLog.Should().ContainInOrder(
            "Behavior1-Before",
            "Behavior2-Before",
            "Behavior3-Before",
            "Handler",
            "Behavior3-After",
            "Behavior2-After",
            "Behavior1-After"
        );
    }

    [Fact]
    public async Task Pipeline_WithNoBehaviors_ShouldExecuteHandlerDirectly()
    {
        // Arrange
        var executionLog = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionLog);
        services.AddScoped<IRequestHandler<LoggingQuery, LoggingResponse>, LoggingQueryHandler>();
        services.AddScoped<IMediator, global::Mediator.Mediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new LoggingQuery());

        // Assert
        result.Should().NotBeNull();
        executionLog.Should().ContainSingle().Which.Should().Be("Handler");
    }

    [Fact]
    public async Task Pipeline_BehaviorCanShortCircuit_ShouldNotCallHandler()
    {
        // Arrange
        var executionLog = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionLog);
        services.AddScoped<IRequestHandler<LoggingQuery, LoggingResponse>, LoggingQueryHandler>();
        services.AddScoped<IPipelineBehavior<LoggingQuery, LoggingResponse>, ShortCircuitBehavior>();
        services.AddScoped<IMediator, global::Mediator.Mediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new LoggingQuery());

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("Short-circuited");
        executionLog.Should().NotContain("Handler");
    }

    [Fact]
    public async Task Pipeline_BehaviorCanModifyResponse_ShouldReturnModifiedResponse()
    {
        // Arrange
        var executionLog = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionLog);
        services.AddScoped<IRequestHandler<LoggingQuery, LoggingResponse>, LoggingQueryHandler>();
        services.AddScoped<IPipelineBehavior<LoggingQuery, LoggingResponse>, ResponseModifyingBehavior>();
        services.AddScoped<IMediator, global::Mediator.Mediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new LoggingQuery());

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("Modified by behavior");
    }

    [Fact]
    public async Task Pipeline_BehaviorThrowsException_ShouldPropagateException()
    {
        // Arrange
        var executionLog = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionLog);
        services.AddScoped<IRequestHandler<LoggingQuery, LoggingResponse>, LoggingQueryHandler>();
        services.AddScoped<IPipelineBehavior<LoggingQuery, LoggingResponse>, ExceptionThrowingBehavior>();
        services.AddScoped<IMediator, global::Mediator.Mediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var act = () => mediator.SendAsync(new LoggingQuery());

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .Where(ex => ex.InnerException is InvalidOperationException && ex.InnerException.Message == "Behavior error");
    }

    // Test fixtures
    public record LoggingQuery : IRequest<LoggingResponse>;

    public record LoggingResponse(string Message);

    public class LoggingQueryHandler(List<string> log) : IRequestHandler<LoggingQuery, LoggingResponse>
    {
        public Task<LoggingResponse> HandleAsync(LoggingQuery request, CancellationToken cancellationToken)
        {
            log.Add("Handler");
            return Task.FromResult(new LoggingResponse("Handled"));
        }
    }

    public class LoggingBehavior1(List<string> log) : IPipelineBehavior<LoggingQuery, LoggingResponse>
    {
        public async Task<LoggingResponse> Handle(LoggingQuery request, RequestHandler<LoggingResponse> nextHandler, CancellationToken cancellationToken)
        {
            log.Add("Behavior1-Before");
            var response = await nextHandler();
            log.Add("Behavior1-After");
            return response;
        }
    }

    public class LoggingBehavior2(List<string> log) : IPipelineBehavior<LoggingQuery, LoggingResponse>
    {
        public async Task<LoggingResponse> Handle(LoggingQuery request, RequestHandler<LoggingResponse> nextHandler, CancellationToken cancellationToken)
        {
            log.Add("Behavior2-Before");
            var response = await nextHandler();
            log.Add("Behavior2-After");
            return response;
        }
    }

    public class LoggingBehavior3(List<string> log) : IPipelineBehavior<LoggingQuery, LoggingResponse>
    {
        public async Task<LoggingResponse> Handle(LoggingQuery request, RequestHandler<LoggingResponse> nextHandler, CancellationToken cancellationToken)
        {
            log.Add("Behavior3-Before");
            var response = await nextHandler();
            log.Add("Behavior3-After");
            return response;
        }
    }

    public class ShortCircuitBehavior : IPipelineBehavior<LoggingQuery, LoggingResponse>
    {
        public Task<LoggingResponse> Handle(LoggingQuery request, RequestHandler<LoggingResponse> nextHandler, CancellationToken cancellationToken)
        {
            // Don't call next - short circuit
            return Task.FromResult(new LoggingResponse("Short-circuited"));
        }
    }

    public class ResponseModifyingBehavior : IPipelineBehavior<LoggingQuery, LoggingResponse>
    {
        public async Task<LoggingResponse> Handle(LoggingQuery request, RequestHandler<LoggingResponse> nextHandler, CancellationToken cancellationToken)
        {
            await nextHandler();
            return new LoggingResponse("Modified by behavior");
        }
    }

    public class ExceptionThrowingBehavior : IPipelineBehavior<LoggingQuery, LoggingResponse>
    {
        public Task<LoggingResponse> Handle(LoggingQuery request, RequestHandler<LoggingResponse> nextHandler, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Behavior error");
        }
    }
}
