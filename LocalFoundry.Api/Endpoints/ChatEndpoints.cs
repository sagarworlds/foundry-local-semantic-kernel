using LocalFoundry.Api.Models;
using Microsoft.Extensions.AI;

namespace LocalFoundry.Api.Endpoints;

/// <summary>
/// Maps the plain chat endpoints, which depend only on the framework-neutral
/// <see cref="IChatClient"/> (backed by Semantic Kernel + Foundry Local).
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
        try
        {
            var response = await chatClient.GetResponseAsync(
                [new ChatMessage(ChatRole.User, request.Message)],
                cancellationToken: ct);

            return Results.Ok(new ChatReply(response.Text));
        }
        catch (Exception ex)
        {
            return FoundryLocalProblems.Unavailable(ex);
        }
    }

    private static async Task StreamChatAsync(ChatRequest request, IChatClient chatClient, HttpResponse httpResponse, CancellationToken ct)
    {
        try
        {
            httpResponse.ContentType = "text/plain";

            await foreach (var update in chatClient.GetStreamingResponseAsync(
                [new ChatMessage(ChatRole.User, request.Message)],
                cancellationToken: ct))
            {
                await httpResponse.WriteAsync(update.Text, ct);
                await httpResponse.Body.FlushAsync(ct);
            }
        }
        catch (Exception ex) when (!httpResponse.HasStarted)
        {
            await FoundryLocalProblems.Unavailable(ex).ExecuteAsync(httpResponse.HttpContext);
        }
    }
}
