using Microsoft.SemanticKernel;

namespace LocalFoundry.Api.Services;

/// <summary>
/// Supplies a Semantic Kernel <see cref="Kernel"/> wired to a particular Foundry Local model.
/// </summary>
public interface IKernelProvider
{
    /// <summary>
    /// Gets a kernel whose chat completion service targets <paramref name="modelId"/>.
    /// </summary>
    /// <param name="modelId">The model alias to use, or <see langword="null"/> for the configured default.</param>
    /// <returns>A kernel with the chat completion service and plugins registered.</returns>
    Kernel GetKernel(string? modelId);
}
