using LocalFoundry.Api.ErrorHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace LocalFoundry.Api.Tests;

public class FoundryLocalExceptionHandlerTests
{
    private readonly RecordingProblemDetailsService _problemDetails = new();
    private readonly FoundryLocalExceptionHandler _handler;

    public FoundryLocalExceptionHandlerTests()
    {
        _handler = new FoundryLocalExceptionHandler(_problemDetails, NullLogger<FoundryLocalExceptionHandler>.Instance);
    }

    [Fact]
    public async Task TryHandleAsync_ClientAbortedRequest_Sets499WithoutWritingABody()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();
        var context = new DefaultHttpContext { RequestAborted = aborted.Token };

        var handled = await _handler.TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(499, context.Response.StatusCode);
        Assert.Null(_problemDetails.Written);
    }

    [Fact]
    public async Task TryHandleAsync_CancellationWithoutClientAbort_IsLeftForTheDefaultHandler()
    {
        // e.g. an internal timeout: not the client's doing, so it must not be reported as 499.
        var context = new DefaultHttpContext();

        var handled = await _handler.TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        Assert.False(handled);
        Assert.Null(_problemDetails.Written);
    }

    [Fact]
    public async Task TryHandleAsync_FoundryLocalFailure_WritesMappedProblemDetails()
    {
        var context = new DefaultHttpContext();

        var handled = await _handler.TryHandleAsync(context, new HttpRequestException("Connection refused"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, _problemDetails.Written!.ProblemDetails.Status);
        Assert.Contains("Connection refused", _problemDetails.Written.ProblemDetails.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_UnrelatedException_IsLeftForTheDefaultHandler()
    {
        var handled = await _handler.TryHandleAsync(new DefaultHttpContext(), new InvalidOperationException("bug"), CancellationToken.None);

        Assert.False(handled);
        Assert.Null(_problemDetails.Written);
    }

    private sealed class RecordingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? Written { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Written = context;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Written = context;
            return ValueTask.FromResult(true);
        }
    }
}
