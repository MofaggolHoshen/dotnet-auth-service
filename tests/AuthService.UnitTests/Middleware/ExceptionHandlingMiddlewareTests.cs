using System.Net;
using System.Text;
using AuthService.API.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuthService.UnitTests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock = new();

    private ExceptionHandlingMiddleware BuildMiddleware(RequestDelegate next) =>
        new(next, _loggerMock.Object);

    private static DefaultHttpContext MakeContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
    }

    // ── No exception ───────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNextAndLeavesStatusCode()
    {
        var middleware = BuildMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        });
        var context = MakeContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }

    // ── ArgumentException → 400 ────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_Returns400WithProblemDetails()
    {
        var middleware = BuildMiddleware(_ => throw new ArgumentException("bad arg"));
        var context = MakeContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.ContentType.Should().Contain("application/problem+json");

        var body = await ReadResponseBody(context);
        body.Should().Contain("400");
        body.Should().Contain("Invalid request");
    }

    // ── UnauthorizedAccessException → 401 ─────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccessException_Returns401()
    {
        var middleware = BuildMiddleware(_ => throw new UnauthorizedAccessException());
        var context = MakeContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        var body = await ReadResponseBody(context);
        body.Should().Contain("401");
    }

    // ── InvalidOperationException → 400 ───────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenInvalidOperationException_Returns400()
    {
        var middleware = BuildMiddleware(_ => throw new InvalidOperationException("bad op"));
        var context = MakeContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    // ── Generic exception → 500 ────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_Returns500()
    {
        var middleware = BuildMiddleware(_ => throw new Exception("boom"));
        var context = MakeContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        var body = await ReadResponseBody(context);
        body.Should().Contain("500");
        body.Should().Contain("unexpected error");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_LogsError()
    {
        var middleware = BuildMiddleware(_ => throw new Exception("boom"));
        var context = MakeContext();

        await middleware.InvokeAsync(context);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── Response body does not expose details on 500 ───────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_DoesNotExposeExceptionMessage()
    {
        var middleware = BuildMiddleware(_ => throw new Exception("sensitive internal message"));
        var context = MakeContext();

        await middleware.InvokeAsync(context);

        var body = await ReadResponseBody(context);
        body.Should().NotContain("sensitive internal message");
    }
}
