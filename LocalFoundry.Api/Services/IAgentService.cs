using Microsoft.Extensions.AI;

namespace LocalFoundry.Api.Services;

/// <summary>
/// Answers a conversation with a model that may call registered tools (function calling).
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Sends the conversation to the model with automatic tool invocation enabled.
    /// </summary>
    /// <param name="messages">The conversation so far, oldest first, ending with the user's new message.</param>
    /// <param name="modelId">Model to use for this request, or <see langword="null"/> for the configured default.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The model's final text reply, or an empty string if it produced none.</returns>
    Task<string> AskAsync(IEnumerable<ChatMessage> messages, string? modelId, CancellationToken cancellationToken);

    /// <summary>
    /// Sends the conversation to the model with automatic tool invocation enabled, yielding the
    /// reply as it is generated. Tool calls happen behind the scenes; only reply text is yielded.
    /// </summary>
    /// <param name="messages">The conversation so far, oldest first, ending with the user's new message.</param>
    /// <param name="modelId">Model to use for this request, or <see langword="null"/> for the configured default.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The non-empty text fragments of the reply, in order.</returns>
    IAsyncEnumerable<string> AskStreamingAsync(IEnumerable<ChatMessage> messages, string? modelId, CancellationToken cancellationToken);
}
