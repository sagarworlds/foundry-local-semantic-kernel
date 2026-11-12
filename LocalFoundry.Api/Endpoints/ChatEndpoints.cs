using LocalFoundry.Api.ErrorHandling;
using LocalFoundry.Api.Models;
using LocalFoundry.Api.Timeouts;
using LocalFoundry.Api.Validation;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.AI;

namespace LocalFoundry.Api.Endpoints;

/// <summary>
/// Maps the plain chat endpoints, which depend only on the framework-neutral
/// <see cref="IChatClient"/> (backed by Semantic Kernel + Foundry Local).
/// Failures are translated to HTTP responses by the global exception handler.
/// </summary>
public static class ChatEndpoints
{
    private const string TokenEvent = "token";
    private const string DoneEvent = "done";
    private const string ErrorEvent = "error";

    /// <summary>Maps <c>POST /api/chat</c> and <c>POST /api/chat/stream</c>.</summary>
    /// <param name="app">The route builder to add the endpoints to.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var chat = app.MapGroup("/api/chat")
            .AddEndpointFilter<ChatRequestValidationFilter>()
            .WithTags("Chat")
            .ProducesValidationProblem()
            .ProducesFoundryLocalProblems();
        chat.WithRequestTimeout(ModelRequestTimeout.PolicyName);

        chat.MapPost("/", ChatAsync)
            .WithName("Chat")
            .WithSummary("Sends a message (with optional history) and returns the model's complete reply.")
            .Produces<ChatReply>();

        chat.MapPost("/stream", StreamChatAsync)
            .WithName("ChatStream")
            .WithSummary("Sends a message and streams the reply as Server-Sent Events.")
            .WithDescription(
                "Emits 'token' events with {\"text\"} as the reply is generated, then a final 'done' event. " +
                "If the stream fails part-way it ends with an 'error' event carrying {\"status\",\"title\",\"detail\"} instead.")
            .Produces<string>(StatusCodes.Status200OK, "text/event-stream");
        return app;
    }

    private static async Task<IResult> ChatAsync(ChatRequest request, IChatClient chatClient, CancellationToken ct)
    {
        var response = await chatClient.GetResponseAsync(request.ToChatMessages(), request.ToChatOptions(), ct);

        return Results.Ok(new ChatReply(response.Text));
    }

    private static async Task StreamChatAsync(
        ChatRequest request, IChatClient chatClient, HttpResponse httpResponse, ILoggerFactory loggerFactory, CancellationToken ct)
    {
        // Pull the first update before committing to a 200 event stream, so that an
        // unreachable Foundry Local still reaches the exception handler as a proper 503.
        await using var updates = chatClient
            .GetStreamingResponseAsync(request.ToChatMessages(), request.ToChatOptions(), ct)
            .GetAsyncEnumerator(ct);

        var hasUpdate = await updates.MoveNextAsync();

        // The timeout guards against a model that never answers. Once tokens are flowing, a
        // long reply is legitimate and must not be cut off mid-sentence.
        httpResponse.HttpContext.Features.Get<IHttpRequestTimeoutFeature>()?.DisableTimeout();

        var events = new ServerSentEventWriter(httpResponse);
        events.Start();

        try
        {
            while (hasUpdate)
            {
                // Updates carrying only metadata (role, usage) have no text worth sending.
                if (!string.IsNullOrEmpty(updates.Current.Text))
                {
                    await events.WriteAsync(TokenEvent, new ChatStreamToken(updates.Current.Text), ct);
                }

                hasUpdate = await updates.MoveNextAsync();
            }

            await events.WriteAsync(DoneEvent, new { }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Client disconnected: nobody is left to tell.
        }
        // The 200 status is already sent, so the exception handler can no longer turn this into
        // an error response. Report it in-band instead; otherwise the client couldn't tell a
        // failed stream from a finished one.
        catch (Exception ex)
        {
            loggerFactory.CreateLogger(typeof(ChatEndpoints)).LogError(ex, "Chat stream failed after the response had started.");
            await events.WriteAsync(ErrorEvent, ToStreamError(ex), CancellationToken.None);
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
