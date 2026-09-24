using System.ClientModel;
using System.Net.Sockets;
using Microsoft.AI.Foundry.Local;
using Microsoft.SemanticKernel;

namespace LocalFoundry.Api.ErrorHandling;

/// <summary>
/// Classifies exceptions raised while talking to Foundry Local. Only failures that are known
/// to come from the inference runtime are mapped; anything else is a bug in this app and is
/// deliberately left unmapped so it surfaces as a 500 instead of being disguised as an outage.
/// </summary>
public static class FoundryLocalErrorMapper
{
    private const string InferenceSetupHint =
        "Run 'foundry model run <alias>' (or 'foundry service start') and check the FoundryLocal:Endpoint/ModelId " +
        "settings in appsettings.json match what's running. See README.md for setup steps.";

    private const string CatalogSetupHint = "Install it and run 'foundry service start' first.";

    /// <summary>
    /// Tries to map <paramref name="exception"/> to a client-facing error.
    /// </summary>
    /// <param name="exception">The exception thrown while handling the request.</param>
    /// <param name="error">The mapped error, or <see langword="null"/> when the exception isn't a known Foundry Local failure.</param>
    /// <returns><see langword="true"/> if the exception was recognised.</returns>
    public static bool TryMap(Exception exception, out FoundryLocalError? error)
    {
        error = exception switch
        {
            // The Foundry Local native SDK (model catalog) failed to start or respond.
            FoundryLocalException ex => new FoundryLocalError(
                StatusCodes.Status503ServiceUnavailable,
                "Foundry Local unavailable",
                $"Could not reach Foundry Local: {ex.Message}. {CatalogSetupHint}"),

            // Foundry Local answered, but with an error (e.g. the configured ModelId isn't loaded).
            // It's an upstream failure rather than this app's, hence 502 Bad Gateway.
            // SK wraps non-streaming calls in HttpOperationException but lets the OpenAI SDK's
            // ClientResultException escape from streaming calls, so both shapes are handled.
            HttpOperationException { StatusCode: not null } ex =>
                UpstreamError((int)ex.StatusCode.Value, ex.ResponseContent ?? ex.Message),
            ClientResultException { Status: > 0 } ex =>
                UpstreamError(ex.Status, ex.GetRawResponse()?.Content.ToString() ?? ex.Message),

            // No HTTP response at all: the daemon isn't running or is on a different port.
            _ when IsConnectionFailure(exception) => new FoundryLocalError(
                StatusCodes.Status503ServiceUnavailable,
                "Foundry Local unavailable",
                $"Could not reach the Foundry Local inference endpoint: {exception.Message}. {InferenceSetupHint}"),

            _ => null,
        };

        return error is not null;
    }

    private static FoundryLocalError UpstreamError(int upstreamStatus, string upstreamMessage) =>
        new(
            StatusCodes.Status502BadGateway,
            "Foundry Local returned an error",
            $"The Foundry Local inference endpoint responded with HTTP {upstreamStatus}: {upstreamMessage}. {InferenceSetupHint}");

    // SK wraps transport failures in HttpOperationException (with no StatusCode), so walk the
    // inner-exception chain rather than matching only the outermost type.
    private static bool IsConnectionFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException or SocketException)
            {
                return true;
            }
        }

        return false;
    }
}
