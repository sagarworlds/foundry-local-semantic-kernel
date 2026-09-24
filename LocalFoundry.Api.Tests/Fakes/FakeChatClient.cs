using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace LocalFoundry.Api.Tests.Fakes;

/// <summary>
/// Scriptable <see cref="IChatClient"/>: returns <see cref="Chunks"/> (joined for non-streaming
/// calls) or throws <see cref="ExceptionToThrow"/> before producing any output.
/// </summary>
public sealed class FakeChatClient : IChatClient
{
    public IReadOnlyList<string> Chunks { get; set; } = ["Paris"];

    public Exception? ExceptionToThrow { get; set; }

    public int CallCount { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        CallCount++;
        ThrowIfConfigured();
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Concat(Chunks))));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        CallCount++;
        await Task.Yield();
        ThrowIfConfigured();

        foreach (var chunk in Chunks)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    private void ThrowIfConfigured()
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }
    }
}
