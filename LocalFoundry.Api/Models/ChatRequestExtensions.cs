using Microsoft.Extensions.AI;

namespace LocalFoundry.Api.Models;

/// <summary>
/// Converts API request DTOs into the framework-neutral Microsoft.Extensions.AI message types.
/// </summary>
public static class ChatRequestExtensions
{
    /// <summary>
    /// Builds the message list for the model: the request's history in order, followed by the new message.
    /// </summary>
    /// <param name="request">A request that has already passed validation.</param>
    /// <returns>The conversation, oldest message first.</returns>
    public static IList<ChatMessage> ToChatMessages(this ChatRequest request)
    {
        var messages = new List<ChatMessage>((request.History?.Count ?? 0) + 1);

        foreach (var turn in request.History ?? [])
        {
            messages.Add(new ChatMessage(ToChatRole(turn.Role), turn.Content));
        }

        messages.Add(new ChatMessage(ChatRole.User, request.Message));
        return messages;
    }

    // Validation only admits "user" and "assistant", so anything that isn't the assistant is the user.
    private static ChatRole ToChatRole(string role) =>
        string.Equals(role, ChatTurnRoles.Assistant, StringComparison.OrdinalIgnoreCase) ? ChatRole.Assistant : ChatRole.User;
}

/// <summary>The role names accepted in <see cref="ChatTurn.Role"/>.</summary>
public static class ChatTurnRoles
{
    /// <summary>A message written by the person.</summary>
    public const string User = "user";

    /// <summary>A reply produced by the model.</summary>
    public const string Assistant = "assistant";

    /// <summary>Whether <paramref name="role"/> is an accepted role name (case-insensitive).</summary>
    public static bool IsValid(string? role) =>
        string.Equals(role, User, StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, Assistant, StringComparison.OrdinalIgnoreCase);
}
