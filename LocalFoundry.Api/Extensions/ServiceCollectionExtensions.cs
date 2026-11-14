using LocalFoundry.Api.Health;
using LocalFoundry.Api.Options;
using LocalFoundry.Api.Plugins;
using LocalFoundry.Api.Services;
using LocalFoundry.Api.Timeouts;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace LocalFoundry.Api.Extensions;

/// <summary>
/// Dependency-injection registration for the Foundry Local / Semantic Kernel / Microsoft.Extensions.AI stack.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Semantic Kernel provider, the <see cref="IChatClient"/> adapter over it,
    /// the agent service, the Foundry Local catalog service and the model-call timeout policy.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">Configuration containing the <c>FoundryLocal</c> and <c>Agent</c> sections.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddLocalFoundryAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<FoundryLocalOptions>, FoundryLocalOptionsValidator>();
        services.AddOptions<FoundryLocalOptions>()
            .Bind(configuration.GetSection("FoundryLocal"))
            .ValidateOnStart();

        services.AddOptions<AgentOptions>()
            .Bind(configuration.GetSection(AgentOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRequestTimeouts();
        services.AddSingleton<IConfigureOptions<RequestTimeoutOptions>, ModelRequestTimeout>();

        // One pooled client for every call to Foundry Local. Its own timeout is switched off
        // because the request-timeout policy above already bounds how long a call may take;
        // HttpClient's default 100 seconds would otherwise cut in first.
        services.AddHttpClient(FoundryLocalKernelProvider.HttpClientName, client => client.Timeout = Timeout.InfiniteTimeSpan);

        services.AddHealthChecks().AddCheck<FoundryLocalHealthCheck>(FoundryLocalHealthCheck.Name);

        // Semantic Kernel: orchestration layer, pointed at Foundry Local's OpenAI-compatible
        // endpoint (run `foundry server start --port 5273` first - see README).
        services.AddSingleton(TimeProvider.System);
        services.AddAgentPlugins();
        services.AddSingleton<IKernelProvider, FoundryLocalKernelProvider>();

        // Microsoft.Extensions.AI: the framework-neutral abstraction endpoint code depends on.
        services.AddSingleton<IChatClient, ModelRoutingChatClient>();

        services.AddSingleton<IAgentService, SemanticKernelAgentService>();
        services.AddSingleton<IModelCatalogService, FoundryLocalCatalogService>();

        return services;
    }

    // The tools the agent may call. To add one, write a plugin class and register it here;
    // the kernel provider picks up every registered KernelPlugin.
    private static void AddAgentPlugins(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
            KernelPluginFactory.CreateFromObject(new TimePlugin(sp.GetRequiredService<TimeProvider>()), nameof(TimePlugin)));
        services.AddSingleton(_ => KernelPluginFactory.CreateFromObject(new MathPlugin(), nameof(MathPlugin)));
    }
}
