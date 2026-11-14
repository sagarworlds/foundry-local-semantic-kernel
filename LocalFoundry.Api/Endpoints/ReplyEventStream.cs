using System.Text;
using LocalFoundry.Api.ErrorHandling;
using LocalFoundry.Api.Models;
using Microsoft.AspNetCore.Http.Timeouts;

namespace LocalFoundry.Api.Endpoints;

/// <summary>
/// Sends a model reply to the client as Server-Sent Events: a <c>token</c> event per text
/// fragment, then <c>done</c>, or <c>error</c> if the reply fails part-way.
/// </summary>
internal static class ReplyEventStream
{
    private const string TokenEvent = "token";
    private const string DoneEvent = "done";
    private const string ErrorEvent = "error";

    /// <summary>
    /// Streams <paramref name="fragments"/> to <paramref name="response"/>.
    /// </summary>
    /// <returns>
    /// The complete reply if the stream finished, or <see langword="null"/> if it failed or the
    /// client disconnected (so callers don't record a partial reply as a real one).
    /// </returns>
    /// <exception cref="Exception">
    /// Anything thrown before the first fragment is rethrown untouched, with no response started,
    /// so the global exception handler can still return a proper error status.
    /// </exception>
    public static async Task<string?> WriteAsync(
        IAsyncEnumerable<string> fragments, HttpResponse response, ILogger logger, CancellationToken ct)
    {
        // Pull the first fragment before committing to a 200 event stream, so that an
        // unreachable Foundry Local still reaches the exception handler as a proper 503.
        await using var enumerator = fragments.GetAsyncEnumerator(ct);
        var hasFragment = await enumerator.MoveNextAsync();

        // The timeout guards against a model that never answers. Once tokens are flowing, a
        // long reply is legitimate and must not be cut off mid-sentence.
        response.HttpContext.Features.Get<IHttpRequestTimeoutFeature>()?.DisableTimeout();

        var events = new ServerSentEventWriter(response);
        events.Start();
        var reply = new StringBuilder();

        try
        {
            while (hasFragment)
            {
                reply.Append(enumerator.Current);
                await events.WriteAsync(TokenEvent, new ChatStreamToken(enumerator.Current), ct);
                hasFragment = await enumerator.MoveNextAsync();
            }

            await events.WriteAsync(DoneEvent, new { }, ct);
            return reply.ToString();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Client disconnected: nobody is left to tell.
            return null;
        }
        // The 200 status is already sent, so the exception handler can no longer turn this into
        // an error response. Report it in-band instead; otherwise the client couldn't tell a
        // failed stream from a finished one.
        catch (Exception ex)
        {
            logger.LogError(ex, "Reply stream failed after the response had started.");
            await events.WriteAsync(ErrorEvent, ToStreamError(ex), CancellationToken.None);
            return null;
        }
    }

    private static ChatStreamError ToStreamError(Exception exception) =>
        FoundryLocalErrorMapper.TryMap(exception, out var error)
            ? new ChatStreamError(error!.StatusCode, error.Title, error.Detail)
            : new ChatStreamError(
                StatusCodes.Status500InternalServerError,
                "Unexpected error",
                "The stream failed unexpectedly. Check the server logs for details.");
}
