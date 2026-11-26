using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LocalFoundry.Api.Tests.Integration;

/// <summary>
/// Hosts the real app in memory, wired to a real Foundry Local (no fakes), shared by the
/// integration tests. Does nothing unless the tests were opted into.
/// </summary>
public sealed class FoundryLocalFixture : IAsyncLifetime
{
    // The app's own configured defaults (appsettings.json), used when no override is given.
    private const string DefaultEndpoint = "http://localhost:5273/v1";
    private const string DefaultModel = "qwen2.5-0.5b";

    private WebApplicationFactory<Program>? _factory;

    /// <summary>Client for the in-memory app. Only valid when the tests are enabled.</summary>
    public HttpClient Client { get; private set; } = null!;

    /// <summary>The model alias the app is configured with for this run.</summary>
    public string Model { get; } = FoundryLocalIntegrationSettings.ModelOverride ?? DefaultModel;

    private string Endpoint { get; } = FoundryLocalIntegrationSettings.EndpointOverride ?? DefaultEndpoint;

    public async Task InitializeAsync()
    {
        if (!FoundryLocalIntegrationSettings.Enabled)
        {
            return;
        }

        await EnsureFoundryLocalAnswersAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("FoundryLocal:Endpoint", Endpoint);
            builder.UseSetting("FoundryLocal:ModelId", Model);
            // The first request can include loading the model from disk, which is slow on modest hardware.
            builder.UseSetting("FoundryLocal:RequestTimeoutSeconds", "240");
        });

        Client = _factory.CreateClient();
        Client.Timeout = TimeSpan.FromMinutes(5);
    }

    public Task DisposeAsync()
    {
        Client?.Dispose();
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    // The tests were asked for, so an unreachable Foundry Local must fail them loudly rather
    // than skip: a green run that tested nothing would be worse than a red one.
    private async Task EnsureFoundryLocalAnswersAsync()
    {
        var modelsUrl = Endpoint.TrimEnd('/') + "/models";

        try
        {
            using var probe = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await probe.GetAsync(modelsUrl);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new InvalidOperationException(
                $"{FoundryLocalIntegrationSettings.EnableVariable} is set, but Foundry Local did not answer at {modelsUrl} ({ex.Message}). " +
                $"Start it and load the model first:{Environment.NewLine}" +
                $"  foundry server start --port 5273{Environment.NewLine}" +
                $"  foundry model download {Model}{Environment.NewLine}" +
                $"  foundry model load {Model}{Environment.NewLine}" +
                $"Use {FoundryLocalIntegrationSettings.EndpointVariable} if it is listening elsewhere.",
                ex);
        }
    }
}
