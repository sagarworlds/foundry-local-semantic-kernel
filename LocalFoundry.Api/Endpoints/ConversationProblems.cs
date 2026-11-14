namespace LocalFoundry.Api.Endpoints;

/// <summary>Problem responses shared by every endpoint that takes a conversation id.</summary>
internal static class ConversationProblems
{
    /// <summary>A 404 for a conversation that doesn't exist, was deleted or has expired.</summary>
    public static IResult NotFound(string conversationId) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Conversation not found",
            detail:
                $"No conversation with id '{conversationId}' exists. It may have expired, been deleted, or been lost when " +
                "the app restarted. Create a new one with POST /api/conversations.");
}
