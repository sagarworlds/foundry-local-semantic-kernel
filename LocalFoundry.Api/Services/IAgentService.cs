namespace LocalFoundry.Api.Services;

/// <summary>
/// Answers a user message with a model that may call registered tools (function calling).
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Sends <paramref name="message"/> to the model with automatic tool invocation enabled.
    /// </summary>
    /// <param name="message">The user's message.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The model's final text reply, or an empty string if it produced none.</returns>
    Task<string> AskAsync(string message, CancellationToken cancellationToken);
}
