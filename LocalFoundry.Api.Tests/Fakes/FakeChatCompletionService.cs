using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LocalFoundry.Api.Tests.Fakes;

/// <summary>
/// Semantic Kernel chat service that returns a fixed reply and records the history, settings
/// and kernel it was called with.
/// </summary>
public sealed class FakeChatCompletionService : IChatCompletionService
{
    public string? Reply { get; set; } = "fake reply";

    public ChatHistory? LastHistory { get; private set; }

    public PromptExecutionSettings? LastSettings { get; private set; }

    public Kernel? LastKernel { get; private set; }

    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
    {
        // Copied because callers may keep mutating the history they passed in.
        LastHistory = [.. chatHistory];
        LastSettings = executionSettings;
        LastKernel = kernel;
        return Task.FromResult<IReadOnlyList<ChatMessageContent>>([new ChatMessageContent(AuthorRole.Assistant, Reply)]);
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
        yield return new StreamingChatMessageContent(AuthorRole.Assistant, Reply);
    }
}
