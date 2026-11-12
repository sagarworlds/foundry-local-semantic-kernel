using LocalFoundry.Api.Options;
using LocalFoundry.Api.Plugins;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace LocalFoundry.Api.Services;

/// <summary>
/// <see cref="IKernelProvider"/> that points Semantic Kernel's OpenAI connector at Foundry
/// Local's OpenAI-compatible endpoint.
/// </summary>
/// <remarks>
/// The connector is bound to one model when it is registered and ignores per-request model
/// ids, so serving a different model requires a kernel built for that model.
/// </remarks>
public sealed class FoundryLocalKernelProvider : IKernelProvider
{
    /// <summary>Name of the <see cref="HttpClient"/> used for all calls to Foundry Local.</summary>
    public const string HttpClientName = "FoundryLocal";

    private readonly FoundryLocalOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Lazy<Kernel> _defaultKernel;

    /// <summary>Creates the provider.</summary>
    /// <param name="options">Endpoint, default model and API key.</param>
    /// <param name="httpClientFactory">Supplies the pooled <see cref="HttpClient"/> the connector sends requests with.</param>
    public FoundryLocalKernelProvider(IOptions<FoundryLocalOptions> options, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _defaultKernel = new Lazy<Kernel>(() => BuildKernel(_options.ModelId));
    }

    /// <inheritdoc />
    public Kernel GetKernel(string? modelId) =>
        modelId is null || string.Equals(modelId, _options.ModelId, StringComparison.OrdinalIgnoreCase)
            ? _defaultKernel.Value
            // Deliberately not cached: model ids come from callers, so a cache keyed on them could
            // grow without bound, and building a kernel is negligible next to model inference.
            // Connections are still pooled because every kernel shares the factory's handler.
            : BuildKernel(modelId);

    private Kernel BuildKernel(string modelId)
    {
        var kernelBuilder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0010 // Custom OpenAI-compatible endpoints are experimental in SK.
        kernelBuilder.AddOpenAIChatCompletion(
            modelId: modelId,
            endpoint: new Uri(_options.Endpoint),
            apiKey: _options.ApiKey,
            httpClient: _httpClientFactory.CreateClient(HttpClientName));
#pragma warning restore SKEXP0010
        kernelBuilder.Plugins.AddFromType<TimePlugin>();

        return kernelBuilder.Build();
    }
}
