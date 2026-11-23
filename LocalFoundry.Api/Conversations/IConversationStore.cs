using LocalFoundry.Api.Models;

namespace LocalFoundry.Api.Conversations;

/// <summary>
/// Keeps conversation transcripts on the server so clients can refer to them by id instead of
/// resending the whole history with each request.
/// </summary>
public interface IConversationStore
{
    /// <summary>Starts a new, empty conversation.</summary>
    /// <returns>The new conversation's id.</returns>
    string Create();

    /// <summary>Gets a conversation's turns, oldest first.</summary>
    /// <param name="id">The conversation id.</param>
    /// <returns>A snapshot of the turns, or <see langword="null"/> if the conversation doesn't exist or has expired.</returns>
    IReadOnlyList<ChatTurn>? Get(string id);

    /// <summary>Appends one completed exchange (the user's message and the model's reply).</summary>
    /// <param name="id">The conversation id.</param>
    /// <param name="userMessage">What the user asked.</param>
    /// <param name="assistantReply">What the model answered.</param>
    /// <returns><see langword="false"/> if the conversation doesn't exist or has expired.</returns>
    bool Append(string id, string userMessage, string assistantReply);

    /// <summary>Deletes a conversation.</summary>
    /// <param name="id">The conversation id.</param>
    /// <returns><see langword="false"/> if the conversation doesn't exist or has expired.</returns>
    bool Delete(string id);
}
