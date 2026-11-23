using LocalFoundry.Api.Conversations;
using LocalFoundry.Api.Models;
using LocalFoundry.Api.Options;
using LocalFoundry.Api.Tests.Fakes;
using LocalFoundry.Api.Validation;

namespace LocalFoundry.Api.Tests;

public class InMemoryConversationStoreTests
{
    private readonly ManualTimeProvider _clock = new();

    private InMemoryConversationStore CreateStore(int maxConversations = 10, int idleTimeoutMinutes = 60) =>
        new(
            Microsoft.Extensions.Options.Options.Create(
                new ConversationOptions { MaxConversations = maxConversations, IdleTimeoutMinutes = idleTimeoutMinutes }),
            _clock);

    [Fact]
    public void Create_ReturnsDistinctIdsThatPassValidation()
    {
        var store = CreateStore();

        var first = store.Create();
        var second = store.Create();

        Assert.NotEqual(first, second);
        Assert.True(ChatRequestValidator.IsValidConversationId(first));
        Assert.Empty(store.Get(first)!);
    }

    [Fact]
    public void Append_RecordsExchangesInOrder()
    {
        var store = CreateStore();
        var id = store.Create();

        Assert.True(store.Append(id, "Capital of France?", "Paris"));
        Assert.True(store.Append(id, "And of Italy?", "Rome"));

        Assert.Equal(
            [new ChatTurn("user", "Capital of France?"), new ChatTurn("assistant", "Paris"), new ChatTurn("user", "And of Italy?"), new ChatTurn("assistant", "Rome")],
            store.Get(id));
    }

    [Fact]
    public void Get_ReturnsASnapshotThatLaterAppendsDoNotChange()
    {
        var store = CreateStore();
        var id = store.Create();
        store.Append(id, "a", "A");

        var snapshot = store.Get(id)!;
        store.Append(id, "b", "B");

        Assert.Equal(2, snapshot.Count);
    }

    [Fact]
    public void UnknownId_IsReportedByEveryOperation()
    {
        var store = CreateStore();

        Assert.Null(store.Get("missing"));
        Assert.False(store.Append("missing", "a", "A"));
        Assert.False(store.Delete("missing"));
    }

    [Fact]
    public void Delete_RemovesTheConversation()
    {
        var store = CreateStore();
        var id = store.Create();

        Assert.True(store.Delete(id));
        Assert.Null(store.Get(id));
        Assert.False(store.Delete(id));
    }

    [Fact]
    public void Append_BeyondTheTurnCap_DropsTheOldestExchangesAndKeepsPairsAligned()
    {
        var store = CreateStore();
        var id = store.Create();
        var exchanges = (ChatRequestValidator.MaxHistoryTurns / 2) + 3;

        for (var i = 0; i < exchanges; i++)
        {
            store.Append(id, $"question {i}", $"answer {i}");
        }

        var turns = store.Get(id)!;
        Assert.Equal(ChatRequestValidator.MaxHistoryTurns, turns.Count);
        Assert.Equal(new ChatTurn("user", "question 3"), turns[0]);
        Assert.Equal(new ChatTurn("assistant", $"answer {exchanges - 1}"), turns[^1]);
    }

    [Fact]
    public void Conversation_UnusedPastTheIdleTimeout_Expires()
    {
        var store = CreateStore(idleTimeoutMinutes: 60);
        var id = store.Create();

        _clock.Advance(TimeSpan.FromMinutes(61));

        Assert.Null(store.Get(id));
        Assert.False(store.Append(id, "a", "A"));
    }

    [Fact]
    public void Conversation_UsedWithinTheIdleTimeout_StaysAlive()
    {
        var store = CreateStore(idleTimeoutMinutes: 60);
        var id = store.Create();

        // Each use restarts the idle clock, so 90 minutes of regular use never expires it.
        _clock.Advance(TimeSpan.FromMinutes(45));
        store.Append(id, "a", "A");
        _clock.Advance(TimeSpan.FromMinutes(45));

        Assert.NotNull(store.Get(id));
    }

    [Fact]
    public void Create_AtCapacity_EvictsTheLeastRecentlyUsedConversation()
    {
        var store = CreateStore(maxConversations: 2);
        var oldest = store.Create();
        _clock.Advance(TimeSpan.FromMinutes(1));
        var newer = store.Create();
        _clock.Advance(TimeSpan.FromMinutes(1));

        // Touching the oldest makes "newer" the least recently used.
        store.Get(oldest);
        _clock.Advance(TimeSpan.FromMinutes(1));
        var newest = store.Create();

        Assert.NotNull(store.Get(oldest));
        Assert.Null(store.Get(newer));
        Assert.NotNull(store.Get(newest));
    }

    [Fact]
    public async Task ConcurrentAppends_ToOneConversation_AreAllRecordedAsWholePairs()
    {
        var store = CreateStore();
        var id = store.Create();

        await Task.WhenAll(Enumerable.Range(0, 20).Select(i => Task.Run(() => store.Append(id, $"q{i}", $"a{i}"))));

        var turns = store.Get(id)!;
        Assert.Equal(40, turns.Count);
        for (var i = 0; i < turns.Count; i += 2)
        {
            Assert.Equal("user", turns[i].Role);
            Assert.Equal("assistant", turns[i + 1].Role);
            Assert.Equal(turns[i].Content[1..], turns[i + 1].Content[1..]);
        }
    }
}
