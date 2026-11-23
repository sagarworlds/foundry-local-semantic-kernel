using System.Collections.Concurrent;
using LocalFoundry.Api.Models;
using LocalFoundry.Api.Options;
using LocalFoundry.Api.Validation;
using Microsoft.Extensions.Options;

namespace LocalFoundry.Api.Conversations;

/// <summary>
/// <see cref="IConversationStore"/> that keeps transcripts in process memory. Conversations are
/// lost when the app restarts, which is acceptable for a local, single-user tool; a durable
/// store can replace this by implementing the same interface.
/// </summary>
public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, Conversation> _conversations = new();
    private readonly TimeProvider _timeProvider;
    private readonly int _maxConversations;
    private readonly TimeSpan _idleTimeout;

    /// <summary>Creates the store.</summary>
    /// <param name="options">Capacity and idle-expiry limits.</param>
    /// <param name="timeProvider">Clock used for idle expiry; injectable so tests can advance time.</param>
    public InMemoryConversationStore(IOptions<ConversationOptions> options, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _maxConversations = options.Value.MaxConversations;
        _idleTimeout = TimeSpan.FromMinutes(options.Value.IdleTimeoutMinutes);
    }

    /// <inheritdoc />
    public string Create()
    {
        MakeRoomForOneMore();

        // "N" format: 32 hex characters, safe in URLs and impractical to guess.
        var id = Guid.NewGuid().ToString("N");
        _conversations[id] = new Conversation(_timeProvider.GetUtcNow());
        return id;
    }

    /// <inheritdoc />
    public IReadOnlyList<ChatTurn>? Get(string id) => Find(id)?.Snapshot(_timeProvider.GetUtcNow());

    /// <inheritdoc />
    public bool Append(string id, string userMessage, string assistantReply)
    {
        var conversation = Find(id);
        if (conversation is null)
        {
            return false;
        }

        conversation.Append(userMessage, assistantReply, _timeProvider.GetUtcNow());
        return true;
    }

    /// <inheritdoc />
    public bool Delete(string id) => Find(id) is not null && _conversations.TryRemove(id, out _);

    private Conversation? Find(string id)
    {
        if (!_conversations.TryGetValue(id, out var conversation))
        {
            return null;
        }

        if (IsExpired(conversation))
        {
            _conversations.TryRemove(id, out _);
            return null;
        }

        return conversation;
    }

    private bool IsExpired(Conversation conversation) =>
        _timeProvider.GetUtcNow() - conversation.LastUsed > _idleTimeout;

    // Runs only when a conversation is created, so its cost (a scan of at most
    // MaxConversations entries) is never paid on the chat path.
    private void MakeRoomForOneMore()
    {
        foreach (var (id, conversation) in _conversations)
        {
            if (IsExpired(conversation))
            {
                _conversations.TryRemove(id, out _);
            }
        }

        while (_conversations.Count >= _maxConversations)
        {
            var leastRecentlyUsed = _conversations.MinBy(entry => entry.Value.LastUsed).Key;
            _conversations.TryRemove(leastRecentlyUsed, out _);
        }
    }

    /// <summary>One transcript. All access goes through its lock because requests for the same conversation can overlap.</summary>
    private sealed class Conversation(DateTimeOffset createdAt)
    {
        private readonly object _gate = new();
        private readonly List<ChatTurn> _turns = [];
        private DateTimeOffset _lastUsed = createdAt;

        public DateTimeOffset LastUsed
        {
            get
            {
                lock (_gate)
                {
                    return _lastUsed;
                }
            }
        }

        public IReadOnlyList<ChatTurn> Snapshot(DateTimeOffset now)
        {
            lock (_gate)
            {
                _lastUsed = now;
                return [.. _turns];
            }
        }

        public void Append(string userMessage, string assistantReply, DateTimeOffset now)
        {
            lock (_gate)
            {
                _turns.Add(new ChatTurn(ChatTurnRoles.User, userMessage));
                _turns.Add(new ChatTurn(ChatTurnRoles.Assistant, assistantReply));

                // Same cap as client-supplied history: the model's context window can't hold more,
                // so the oldest exchanges fall off and the transcript stays bounded.
                var excess = _turns.Count - ChatRequestValidator.MaxHistoryTurns;
                if (excess > 0)
                {
                    // Turns are stored in user/assistant pairs; removing an even number keeps
                    // the transcript starting with a user turn.
                    _turns.RemoveRange(0, excess + (excess % 2));
                }

                _lastUsed = now;
            }
        }
    }
}
