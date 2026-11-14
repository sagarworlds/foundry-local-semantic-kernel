using System.Runtime.CompilerServices;
using LocalFoundry.Api.Services;
using Microsoft.Extensions.AI;

namespace LocalFoundry.Api.Tests.Fakes;

/// <summary>Scriptable <see cref="IAgentService"/> that returns a fixed reply or throws, recording what it was asked.</summary>
public sealed class FakeAgentService : IAgentService
{
    public string Reply { get; set; } = "It is noon.";

    public Exception? ExceptionToThrow { get; set; }

    public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];

    public string? LastModelId { get; private set; }

    public Task<string> AskAsync(IEnumerable<ChatMessage> messages, string? modelId, CancellationToken cancellationToken)
    {
        LastMessages = messages.ToList();
        LastModelId = modelId;
        return ExceptionToThrow is null ? Task.FromResult(Reply) : Task.FromException<string>(ExceptionToThrow);
    }

    public async IAsyncEnumerable<string> AskStreamingAsync(
        IEnumerable<ChatMessage> messages, string? modelId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        LastMessages = messages.ToList();
        LastModelId = modelId;
        await Task.Yield();

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        // Split so the stream has more than one token to assert on.
        foreach (var word in Reply.Split(' '))
        {
            yield return word + " ";
        }
    }
}
