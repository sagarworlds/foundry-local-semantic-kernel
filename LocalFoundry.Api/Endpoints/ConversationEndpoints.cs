using LocalFoundry.Api.Conversations;
using LocalFoundry.Api.Models;
using LocalFoundry.Api.Validation;

namespace LocalFoundry.Api.Endpoints;

/// <summary>Maps the endpoints that manage server-side conversations.</summary>
public static class ConversationEndpoints
{
    /// <summary>
    /// Maps <c>POST /api/conversations</c>, <c>GET /api/conversations/{id}</c> and
    /// <c>DELETE /api/conversations/{id}</c>.
    /// </summary>
    /// <param name="app">The route builder to add the endpoints to.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var conversations = app.MapGroup("/api/conversations").WithTags("Conversations");

        conversations.MapPost("/", Create)
            .WithName("CreateConversation")
            .WithSummary("Starts an empty server-side conversation and returns its id.")
            .Produces<ConversationCreated>(StatusCodes.Status201Created);

        conversations.MapGet("/{id}", Get)
            .WithName("GetConversation")
            .WithSummary("Returns a conversation's recorded turns, oldest first.")
            .Produces<ConversationTranscript>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        conversations.MapDelete("/{id}", Delete)
            .WithName("DeleteConversation")
            .WithSummary("Deletes a conversation.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static IResult Create(IConversationStore store)
    {
        var id = store.Create();
        return Results.Created($"/api/conversations/{id}", new ConversationCreated(id));
    }

    // A malformed id can't name a stored conversation, so it gets the same 404 as an unknown one.
    private static IResult Get(string id, IConversationStore store) =>
        ChatRequestValidator.IsValidConversationId(id) && store.Get(id) is { } turns
            ? Results.Ok(new ConversationTranscript(id, turns))
            : ConversationProblems.NotFound(id);

    private static IResult Delete(string id, IConversationStore store) =>
        ChatRequestValidator.IsValidConversationId(id) && store.Delete(id)
            ? Results.NoContent()
            : ConversationProblems.NotFound(id);
}
