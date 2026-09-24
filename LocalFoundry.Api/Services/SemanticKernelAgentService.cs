using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace LocalFoundry.Api.Services;

/// <summary>
/// <see cref="IAgentService"/> backed by a Semantic Kernel <see cref="Kernel"/>, whose registered
/// plugins are exposed to the model as callable tools.
/// </summary>
public sealed class SemanticKernelAgentService : IAgentService
{
    private const string SystemPrompt = "You are a helpful assistant. Use tools when they help answer the question.";

    private readonly Kernel _kernel;

    /// <summary>Creates the service.</summary>
    /// <param name="kernel">Kernel providing the chat service and the plugins the model may call.</param>
    public SemanticKernelAgentService(Kernel kernel)
    {
        _kernel = kernel;
    }

    /// <inheritdoc />
    public async Task<string> AskAsync(string message, CancellationToken cancellationToken)
    {
        var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory(SystemPrompt);
        history.AddUserMessage(message);

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };

        var result = await chatCompletionService.GetChatMessageContentAsync(history, settings, _kernel, cancellationToken);
        return result.Content ?? string.Empty;
    }
}
