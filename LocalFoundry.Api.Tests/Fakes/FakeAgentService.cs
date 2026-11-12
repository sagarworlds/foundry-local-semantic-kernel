using LocalFoundry.Api.Services;
using Microsoft.Extensions.AI;

namespace LocalFoundry.Api.Tests.Fakes;

/// <summary>Scriptable <see cref="IAgentService"/> that returns a fixed reply or throws, recording what it was asked.</summary>
public sealed class FakeAgentService : IAgentService
{
    public string Reply { get; set; } = "It is noon.";

    public Exception? ExceptionToThrow { get; set; }

    public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];

    public Task<string> AskAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        LastMessages = messages.ToList();
        return ExceptionToThrow is null ? Task.FromResult(Reply) : Task.FromException<string>(ExceptionToThrow);
    }
}
