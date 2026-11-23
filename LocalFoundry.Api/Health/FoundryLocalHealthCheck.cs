using System.Text.Json;
using System.Text.Json.Serialization;
using LocalFoundry.Api.Options;
using LocalFoundry.Api.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LocalFoundry.Api.Health;

/// <summary>
/// Reports whether Foundry Local's OpenAI-compatible endpoint is reachable and whether the
/// configured model is loaded, by listing its models (<c>GET {Endpoint}/models</c>), which is
/// cheap and doesn't run inference.
/// </summary>
public sealed class FoundryLocalHealthCheck : IHealthCheck
{
    /// <summary>Name the check is registered under and reported as.</summary>
    public const string Name = "foundry-local";

    // A local daemon answers a model listing in milliseconds; a probe that takes longer than
    // this should fail fast rather than make the health endpoint itself hang.
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(3);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Uri _modelsUri;
    private readonly string _modelId;

    /// <summary>Creates the check.</summary>
    /// <param name="httpClientFactory">Supplies the pooled client used for Foundry Local calls.</param>
    /// <param name="options">Supplies the Foundry Local endpoint.</param>
    public FoundryLocalHealthCheck(IHttpClientFactory httpClientFactory, IOptions<FoundryLocalOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _modelsUri = new Uri(options.Value.Endpoint.TrimEnd('/') + "/models");
        _modelId = options.Value.ModelId;
    }

    private static async Task<IReadOnlyList<string>> ReadModelIdsAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var list = await response.Content.ReadFromJsonAsync<ModelList>(cancellationToken);
        return list?.Data?.Select(model => model.Id).OfType<string>().ToList() ?? [];
    }

    // A missing model is Degraded rather than Unhealthy: the runtime itself is up, requests that
    // name another loaded model still work, and the match below is a heuristic.
    private HealthCheckResult DescribeModelState(IReadOnlyList<string> loadedModels)
    {
        var authority = _modelsUri.GetLeftPart(UriPartial.Authority);

        if (loadedModels.Any(IsConfiguredModel))
        {
            return HealthCheckResult.Healthy($"Foundry Local is reachable at {authority} and model '{_modelId}' is loaded.");
        }

        var loaded = loadedModels.Count > 0 ? string.Join(", ", loadedModels) : "none";
        return HealthCheckResult.Degraded(
            $"Foundry Local is reachable at {authority}, but the configured model '{_modelId}' does not appear to be loaded " +
            $"(loaded: {loaded}). Run 'foundry model load {_modelId}' or change FoundryLocal:ModelId.");
    }

    // Configuration holds an alias ("qwen2.5-0.5b") while Foundry Local lists full variant ids
    // ("qwen2.5-0.5b-instruct-generic-cpu:4"), so a variant counts as the alias when it starts with it.
    private bool IsConfiguredModel(string loadedModelId) =>
        loadedModelId.StartsWith(_modelId, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ProbeTimeout);

        try
        {
            var client = _httpClientFactory.CreateClient(FoundryLocalKernelProvider.HttpClientName);
            using var response = await client.GetAsync(_modelsUri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Unhealthy($"Foundry Local responded with HTTP {(int)response.StatusCode} to {_modelsUri}.");
            }

            var loadedModels = await ReadModelIdsAsync(response, timeout.Token);
            return DescribeModelState(loadedModels);
        }
        // Reachable but not speaking the expected protocol: most likely the port belongs to something else.
        catch (JsonException ex)
        {
            return HealthCheckResult.Unhealthy($"{_modelsUri} did not return a valid OpenAI model list: {ex.Message}", ex);
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Could not reach Foundry Local at {_modelsUri}: {ex.Message}. Start it with 'foundry server start --port <port>'.", ex);
        }
        // Only the probe's own timeout is a health result; a cancelled health request propagates.
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy($"Foundry Local did not respond at {_modelsUri} within {ProbeTimeout.TotalSeconds:0} seconds.");
        }
    }

    // The parts of the OpenAI "list models" response this check reads.
    private sealed record ModelList([property: JsonPropertyName("data")] IReadOnlyList<ModelEntry>? Data);

    private sealed record ModelEntry([property: JsonPropertyName("id")] string? Id);
}
