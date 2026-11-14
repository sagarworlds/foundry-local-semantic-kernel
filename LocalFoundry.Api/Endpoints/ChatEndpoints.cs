using LocalFoundry.Api.Conversations;
using LocalFoundry.Api.Models;
using LocalFoundry.Api.Timeouts;
using LocalFoundry.Api.Validation;
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
        var chat = app.MapGroup("/api/chat")
            .AddEndpointFilter<ChatRequestValidationFilter>()
            .WithTags("Chat")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesFoundryLocalProblems();
        chat.WithRequestTimeout(ModelRequestTimeout.PolicyName);

        chat.MapPost("/", ChatAsync)
            .WithName("Chat")
            .WithSummary("Sends a message (with optional history) and returns the model's complete reply.")
            .Produces<ChatReply>();

        chat.MapPost("/stream", StreamChatAsync)
            .WithName("ChatStream")
            .WithSummary("Sends a message and streams the reply as Server-Sent Events.")
            .ProducesReplyEventStream();
        return app;
    }

    private static async Task<IResult> ChatAsync(
        ChatRequest request, IChatClient chatClient, IConversationStore conversations, CancellationToken ct)
    {
        if (ConversationSession.Begin(request, conversations) is not { } session)
        {
            return ConversationProblems.NotFound(request.ConversationId!);
        }

        var response = await chatClient.GetResponseAsync(session.Messages, request.ToChatOptions(), ct);
        session.Complete(response.Text);

        return Results.Ok(new ChatReply(response.Text, session.ConversationId));
    }

    private static async Task<IResult> StreamChatAsync(
        ChatRequest request,
        IChatClient chatClient,
        IConversationStore conversations,
        HttpResponse httpResponse,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        if (ConversationSession.Begin(request, conversations) is not { } session)
        {
            return ConversationProblems.NotFound(request.ConversationId!);
        }

        var reply = await ReplyEventStream.WriteAsync(
            TextFragmentsAsync(chatClient.GetStreamingResponseAsync(session.Messages, request.ToChatOptions(), ct)),
            httpResponse,
            loggerFactory.CreateLogger(typeof(ChatEndpoints)),
            ct);

        // null means the stream failed or the client left: don't record a partial reply.
        if (reply is not null)
        {
            session.Complete(reply);
        }

        // The event stream has already been written to the response.
        return Results.Empty;
    }

    // Updates carrying only metadata (role, usage) have no text worth sending.
    private static async IAsyncEnumerable<string> TextFragmentsAsync(IAsyncEnumerable<ChatResponseUpdate> updates)
    {
        await foreach (var update in updates)
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return update.Text;
            }
        }
    }
}
