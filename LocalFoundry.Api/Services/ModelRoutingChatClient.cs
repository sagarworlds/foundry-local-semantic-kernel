using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LocalFoundry.Api.Services;

/// <summary>
/// <see cref="IChatClient"/> that honours <see cref="ChatOptions.ModelId"/> by delegating each
/// call to the Semantic Kernel chat service for that model. This keeps endpoint code on the
/// framework-neutral Microsoft.Extensions.AI abstraction while still allowing a per-request model.
/// </summary>
public sealed class ModelRoutingChatClient : IChatClient
{
    private readonly IKernelProvider _kernelProvider;

    /// <summary>Creates the client.</summary>
    /// <param name="kernelProvider">Supplies the kernel for the requested model.</param>
    public ModelRoutingChatClient(IKernelProvider kernelProvider)
    {
        _kernelProvider = kernelProvider;
    }

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        ClientFor(options).GetResponseAsync(messages, options, cancellationToken);

    /// <inheritdoc />
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        ClientFor(options).GetStreamingResponseAsync(messages, options, cancellationToken);

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    /// <inheritdoc />
    public void Dispose()
    {
        // Owns nothing: kernels and their HTTP handlers belong to the provider and the HttpClient factory.
    }

    // Microsoft.Extensions.AI: adapt the Semantic Kernel chat service into the M.E.AI abstraction.
    private IChatClient ClientFor(ChatOptions? options) =>
        _kernelProvider.GetKernel(options?.ModelId).GetRequiredService<IChatCompletionService>().AsChatClient();
}
