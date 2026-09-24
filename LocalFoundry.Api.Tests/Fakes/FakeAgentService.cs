using LocalFoundry.Api.Services;

namespace LocalFoundry.Api.Tests.Fakes;

/// <summary>Scriptable <see cref="IAgentService"/> that echoes a fixed reply or throws.</summary>
public sealed class FakeAgentService : IAgentService
{
    public string Reply { get; set; } = "It is noon.";

    public Exception? ExceptionToThrow { get; set; }

    public Task<string> AskAsync(string message, CancellationToken cancellationToken) =>
        ExceptionToThrow is null ? Task.FromResult(Reply) : Task.FromException<string>(ExceptionToThrow);
}
