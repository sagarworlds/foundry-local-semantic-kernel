using LocalFoundry.Api.Models;
using Microsoft.Extensions.AI;

namespace LocalFoundry.Api.Conversations;

/// <summary>
/// The conversation context of one chat or agent request: where the earlier turns come from
/// (the request's own <c>history</c>, or the server-side conversation it names) and where a
/// completed exchange is recorded.
/// </summary>
public sealed class ConversationSession
{
    private readonly IConversationStore _store;
    private readonly ChatRequest _request;

    private ConversationSession(IConversationStore store, ChatRequest request, IList<ChatMessage> messages)
    {
        _store = store;
        _request = request;
        Messages = messages;
    }

    /// <summary>The messages to send to the model: earlier turns, then the new message.</summary>
    public IList<ChatMessage> Messages { get; }

    /// <summary>The server-side conversation this request belongs to, or <see langword="null"/> for a stateless request.</summary>
    public string? ConversationId => _request.ConversationId;

    /// <summary>
    /// Resolves the conversation context for <paramref name="request"/>.
    /// </summary>
    /// <param name="request">A request that has already passed validation.</param>
    /// <param name="store">The conversation store.</param>
    /// <returns>The session, or <see langword="null"/> if the request names a conversation that doesn't exist or has expired.</returns>
    public static ConversationSession? Begin(ChatRequest request, IConversationStore store)
    {
        if (request.ConversationId is null)
        {
            return new ConversationSession(store, request, request.ToChatMessages(request.History));
        }

        var storedTurns = store.Get(request.ConversationId);
        return storedTurns is null ? null : new ConversationSession(store, request, request.ToChatMessages(storedTurns));
    }

    /// <summary>
    /// Records a completed exchange in the server-side conversation, if the request named one.
    /// Call only after the model replied in full, so failed or partial replies are never stored.
    /// </summary>
    /// <param name="reply">The model's complete reply.</param>
    public void Complete(string reply)
    {
        if (ConversationId is not null)
        {
            // A false result means the conversation was deleted or expired mid-request; the
            // reply has already been produced, so there is nothing useful left to do with it.
            _store.Append(ConversationId, _request.Message, reply);
        }
    }
}
