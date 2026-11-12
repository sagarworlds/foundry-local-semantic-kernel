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
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The model's final text reply, or an empty string if it produced none.</returns>
    Task<string> AskAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken);
}
