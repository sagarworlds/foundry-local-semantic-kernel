using LocalFoundry.Api.Conversations;
using LocalFoundry.Api.Models;
using LocalFoundry.Api.Validation;
using LocalFoundry.Api.Services;
using LocalFoundry.Api.Timeouts;

namespace LocalFoundry.Api.Endpoints;

/// <summary>
/// Maps the tool-calling agent endpoints.
/// Failures are translated to HTTP responses by the global exception handler.
/// </summary>
public static class AgentEndpoints
{
    /// <summary>Maps <c>POST /api/agent</c> and <c>POST /api/agent/stream</c>.</summary>
    /// <param name="app">The route builder to add the endpoints to.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var agent = app.MapGroup("/api/agent")
            .AddEndpointFilter<ChatRequestValidationFilter>()
            .WithTags("Agent")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesFoundryLocalProblems();
        agent.WithRequestTimeout(ModelRequestTimeout.PolicyName);

        agent.MapPost("/", AskAgentAsync)
            .WithName("AskAgent")
            .WithSummary("Sends a message to a model that may call tools (function calling) before replying.")
            .Produces<ChatReply>();

        agent.MapPost("/stream", StreamAgentAsync)
            .WithName("AskAgentStream")
            .WithSummary("Same as the agent endpoint, but streams the reply as Server-Sent Events.")
            .ProducesReplyEventStream();
        return app;
    }

    private static async Task<IResult> AskAgentAsync(
        ChatRequest request, IAgentService agent, IConversationStore conversations, CancellationToken ct)
    {
        if (ConversationSession.Begin(request, conversations) is not { } session)
        {
            return ConversationProblems.NotFound(request.ConversationId!);
        }

        var reply = await agent.AskAsync(session.Messages, request.Model, ct);
        session.Complete(reply);

        return Results.Ok(new ChatReply(reply, session.ConversationId));
    }

    private static async Task<IResult> StreamAgentAsync(
        ChatRequest request,
        IAgentService agent,
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
            agent.AskStreamingAsync(session.Messages, request.Model, ct),
            httpResponse,
            loggerFactory.CreateLogger(typeof(AgentEndpoints)),
            ct);

        // null means the stream failed or the client left: don't record a partial reply.
        if (reply is not null)
        {
            session.Complete(reply);
        }

        // The event stream has already been written to the response.
        return Results.Empty;
    }
}
