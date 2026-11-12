using Microsoft.Extensions.AI;
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
    private const string SystemPrompt = "You are a helpful assistant. Use tools when they help answer the question.";

    private readonly IKernelProvider _kernelProvider;

    /// <summary>Creates the service.</summary>
    /// <param name="kernelProvider">Supplies the kernel (chat service plus callable plugins) for the requested model.</param>
    public SemanticKernelAgentService(IKernelProvider kernelProvider)
    {
        _kernelProvider = kernelProvider;
    }

    /// <inheritdoc />
    public async Task<string> AskAsync(IEnumerable<ChatMessage> messages, string? modelId, CancellationToken cancellationToken)
    {
        var kernel = _kernelProvider.GetKernel(modelId);
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory(SystemPrompt);

        foreach (var message in messages)
        {
            history.AddMessage(ToAuthorRole(message.Role), message.Text);
        }

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };

        var result = await chatCompletionService.GetChatMessageContentAsync(history, settings, kernel, cancellationToken);
        return result.Content ?? string.Empty;
    }

    // Only user and assistant turns are accepted from callers; the system prompt is added above.
    private static AuthorRole ToAuthorRole(ChatRole role) =>
        role == ChatRole.Assistant ? AuthorRole.Assistant : AuthorRole.User;
}
