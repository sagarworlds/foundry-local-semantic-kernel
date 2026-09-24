using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LocalFoundry.Api.ErrorHandling;

/// <summary>
/// Global exception handler that turns known Foundry Local failures into ProblemDetails
/// responses. Unrecognised exceptions are passed on to the default handler (500).
/// </summary>
public sealed partial class FoundryLocalExceptionHandler : IExceptionHandler
{
    // Non-standard but widely used (nginx) status for "client closed request"; nobody reads it,
    // it just keeps these out of the 5xx error metrics.
    private const int ClientClosedRequest = 499;

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<FoundryLocalExceptionHandler> _logger;

    /// <summary>Creates the handler.</summary>
    /// <param name="problemDetailsService">Writes the ProblemDetails response body.</param>
    /// <param name="logger">Logs each handled failure.</param>
    public FoundryLocalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<FoundryLocalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            LogRequestAborted(httpContext.Request.Path);
            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        if (!FoundryLocalErrorMapper.TryMap(exception, out var error))
        {
            return false;
        }

        LogFoundryLocalFailure(exception, httpContext.Request.Path, error!.StatusCode);

        httpContext.Response.StatusCode = error.StatusCode;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = error.StatusCode,
                Title = error.Title,
                Detail = error.Detail,
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Foundry Local call failed for {Path}; returning {StatusCode}.")]
    private partial void LogFoundryLocalFailure(Exception exception, PathString path, int statusCode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Client aborted request to {Path}.")]
    private partial void LogRequestAborted(PathString path);
}
