using LocalFoundry.Api.Options;
using LocalFoundry.Api.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LocalFoundry.Api.Health;

/// <summary>
/// Reports whether Foundry Local's OpenAI-compatible endpoint is reachable by listing its
/// models (<c>GET {Endpoint}/models</c>), which is cheap and doesn't run inference.
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

    /// <summary>Creates the check.</summary>
    /// <param name="httpClientFactory">Supplies the pooled client used for Foundry Local calls.</param>
    /// <param name="options">Supplies the Foundry Local endpoint.</param>
    public FoundryLocalHealthCheck(IHttpClientFactory httpClientFactory, IOptions<FoundryLocalOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _modelsUri = new Uri(options.Value.Endpoint.TrimEnd('/') + "/models");
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ProbeTimeout);

        try
        {
            var client = _httpClientFactory.CreateClient(FoundryLocalKernelProvider.HttpClientName);
            using var response = await client.GetAsync(_modelsUri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"Foundry Local is reachable at {_modelsUri.GetLeftPart(UriPartial.Authority)}.")
                : HealthCheckResult.Unhealthy($"Foundry Local responded with HTTP {(int)response.StatusCode} to {_modelsUri}.");
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
}
