using LocalFoundry.Api.Models;
using Microsoft.Extensions.AI;

namespace LocalFoundry.Api.Endpoints;

/// <summary>
/// Maps the plain chat endpoints, which depend only on the framework-neutral
/// <see cref="IChatClient"/> (backed by Semantic Kernel + Foundry Local).
/// Failures are translated to HTTP responses by the global exception handler.
/// </summary>
public static class ChatEndpoints
{
    /// <summary>Maps <c>POST /api/chat</c> and <c>POST /api/chat/stream</c>.</summary>
    /// <param name="app">The route builder to add the endpoints to.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chat", ChatAsync);
        app.MapPost("/api/chat/stream", StreamChatAsync);
        return app;
    }

    private static async Task<IResult> ChatAsync(ChatRequest request, IChatClient chatClient, CancellationToken ct)
    {
        var response = await chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, request.Message)],
            cancellationToken: ct);

        return Results.Ok(new ChatReply(response.Text));
    }

    private static async Task StreamChatAsync(ChatRequest request, IChatClient chatClient, HttpResponse httpResponse, CancellationToken ct)
    {
        // Pull the first update before committing to a 200 text/plain response, so that an
        // unreachable Foundry Local still reaches the exception handler as a proper 503.
        // Once streaming has begun, a failure can only abort the connection.
        await using var updates = chatClient.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, request.Message)],
            cancellationToken: ct).GetAsyncEnumerator(ct);

        if (!await updates.MoveNextAsync())
        {
            return;
        }

        httpResponse.ContentType = "text/plain";
        do
        {
            await httpResponse.WriteAsync(updates.Current.Text, ct);
            await httpResponse.Body.FlushAsync(ct);
        }
        while (await updates.MoveNextAsync());
    }
}
