using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace LocalFoundry.Api.Tests.Fakes;

/// <summary>
/// Scriptable <see cref="IChatClient"/>: returns <see cref="Chunks"/> (joined for non-streaming
/// calls), throws <see cref="ExceptionToThrow"/> before producing any output, or throws
/// <see cref="ExceptionAfterChunks"/> part-way through a stream.
/// </summary>
public sealed class FakeChatClient : IChatClient
{
    public IReadOnlyList<string> Chunks { get; set; } = ["Paris"];

    public Exception? ExceptionToThrow { get; set; }

    /// <summary>Thrown by streaming calls after all <see cref="Chunks"/> have been yielded.</summary>
    public Exception? ExceptionAfterChunks { get; set; }

    /// <summary>Simulated time the model takes before answering (or before its first chunk).</summary>
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    public int CallCount { get; private set; }

    public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastMessages = messages.ToList();
        await Task.Delay(Delay, cancellationToken);
        ThrowIfConfigured();
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Concat(Chunks)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastMessages = messages.ToList();
        await Task.Delay(Delay, cancellationToken);
        ThrowIfConfigured();

        foreach (var chunk in Chunks)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);
        }

        if (ExceptionAfterChunks is not null)
        {
            throw ExceptionAfterChunks;
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
