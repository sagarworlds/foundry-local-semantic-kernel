using System.Runtime.CompilerServices;
using LocalFoundry.Api.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace LocalFoundry.Api.Services;

/// <summary>
/// <see cref="IAgentService"/> backed by Semantic Kernel, whose registered plugins are exposed
/// to the model as callable tools.
/// </summary>
public sealed class SemanticKernelAgentService : IAgentService
{
    private readonly IKernelProvider _kernelProvider;
    private readonly AgentOptions _options;

    /// <summary>Creates the service.</summary>
    /// <param name="kernelProvider">Supplies the kernel (chat service plus callable plugins) for the requested model.</param>
    /// <param name="options">Supplies the system prompt.</param>
    public SemanticKernelAgentService(IKernelProvider kernelProvider, IOptions<AgentOptions> options)
    {
        _kernelProvider = kernelProvider;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<string> AskAsync(IEnumerable<ChatMessage> messages, string? modelId, CancellationToken cancellationToken)
    {
        var kernel = _kernelProvider.GetKernel(modelId);
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        var result = await chatCompletionService.GetChatMessageContentAsync(
            BuildHistory(messages), ToolCallingSettings(), kernel, cancellationToken);
        return result.Content ?? string.Empty;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> AskStreamingAsync(
        IEnumerable<ChatMessage> messages, string? modelId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var kernel = _kernelProvider.GetKernel(modelId);
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        await foreach (var update in chatCompletionService.GetStreamingChatMessageContentsAsync(
            BuildHistory(messages), ToolCallingSettings(), kernel, cancellationToken))
        {
            // Updates that only carry a tool call or metadata have no reply text.
            if (!string.IsNullOrEmpty(update.Content))
            {
                yield return update.Content;
            }
        }
    }

    private ChatHistory BuildHistory(IEnumerable<ChatMessage> messages)
    {
        var history = new ChatHistory(_options.SystemPrompt);

        foreach (var message in messages)
        {
            history.AddMessage(ToAuthorRole(message.Role), message.Text);
        }

        return history;
    }

    // Auto lets the model decide when to call a plugin function; SK runs it and feeds the result back.
    private static OpenAIPromptExecutionSettings ToolCallingSettings() =>
        new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };

    // Only user and assistant turns are accepted from callers; the system prompt is added above.
    private static AuthorRole ToAuthorRole(ChatRole role) =>
        role == ChatRole.Assistant ? AuthorRole.Assistant : AuthorRole.User;
}
