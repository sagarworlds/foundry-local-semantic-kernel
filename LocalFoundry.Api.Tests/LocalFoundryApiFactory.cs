using LocalFoundry.Api.Services;
using LocalFoundry.Api.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LocalFoundry.Api.Tests;

/// <summary>
/// Hosts the real app in-memory with the Foundry Local-facing services replaced by fakes,
/// so the full HTTP pipeline (routing, validation, exception handling) is exercised
/// without a running Foundry Local.
/// </summary>
public sealed class LocalFoundryApiFactory : WebApplicationFactory<Program>
{
    public FakeChatClient ChatClient { get; } = new();

    public FakeAgentService Agent { get; } = new();

    public FakeModelCatalogService Catalog { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IChatClient>();
            services.RemoveAll<IAgentService>();
            services.RemoveAll<IModelCatalogService>();

            services.AddSingleton<IChatClient>(ChatClient);
            services.AddSingleton<IAgentService>(Agent);
            services.AddSingleton<IModelCatalogService>(Catalog);
        });
    }
}
