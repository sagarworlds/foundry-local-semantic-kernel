using LocalFoundry.Api.Options;
using LocalFoundry.Api.Plugins;
using LocalFoundry.Api.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LocalFoundry.Api.Extensions;

/// <summary>
/// Dependency-injection registration for the Foundry Local / Semantic Kernel / Microsoft.Extensions.AI stack.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Semantic Kernel <see cref="Kernel"/>, the <see cref="IChatClient"/> adapter over it,
    /// the agent service and the Foundry Local catalog service.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">Configuration containing the <c>FoundryLocal</c> section.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddLocalFoundryAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FoundryLocalOptions>(configuration.GetSection("FoundryLocal"));

        // Semantic Kernel: orchestration layer. Points at Foundry Local's OpenAI-compatible
        // endpoint (run `foundry server start --port 5273` first - see README).
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<FoundryLocalOptions>>().Value;

            var kernelBuilder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0010 // Custom OpenAI-compatible endpoints are experimental in SK.
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: options.ModelId,
                endpoint: new Uri(options.Endpoint),
                apiKey: options.ApiKey);
#pragma warning restore SKEXP0010
            kernelBuilder.Plugins.AddFromType<TimePlugin>();

            return kernelBuilder.Build();
        });

        // Microsoft.Extensions.AI: adapt the Semantic Kernel chat service into the M.E.AI
        // abstraction so app/endpoint code can depend on the framework-neutral IChatClient.
        services.AddSingleton<IChatClient>(sp =>
            sp.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>().AsChatClient());

        services.AddSingleton<IAgentService, SemanticKernelAgentService>();
        services.AddSingleton<IModelCatalogService, FoundryLocalCatalogService>();

        return services;
    }
}
