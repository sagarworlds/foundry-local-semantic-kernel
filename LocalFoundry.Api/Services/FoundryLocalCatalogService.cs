using LocalFoundry.Api.Models;
using Microsoft.AI.Foundry.Local;

namespace LocalFoundry.Api.Services;

/// <summary>
/// <see cref="IModelCatalogService"/> backed by the Foundry Local native SDK.
/// The SDK is initialized lazily on first use so the app can still start
/// (and serve /api/chat via the REST endpoint) even if Foundry Local isn't installed yet.
/// </summary>
public sealed class FoundryLocalCatalogService : IModelCatalogService
{
    private readonly ILogger<FoundryLocalCatalogService> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private FoundryLocalManager? _manager;

    /// <summary>Creates the service. No connection to Foundry Local is made until first use.</summary>
    /// <param name="logger">Logger passed to the Foundry Local SDK.</param>
    public FoundryLocalCatalogService(ILogger<FoundryLocalCatalogService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FoundryModelSummary>> ListModelsAsync(CancellationToken cancellationToken)
    {
        var manager = await EnsureManagerAsync(cancellationToken);
        var catalog = await manager.GetCatalogAsync(cancellationToken);
        var models = await catalog.ListModelsAsync(cancellationToken);
        return models.Select(m => new FoundryModelSummary(m.Alias, m.Id)).ToList();
    }

    private async Task<FoundryLocalManager> EnsureManagerAsync(CancellationToken cancellationToken)
    {
        if (_manager is not null)
        {
            return _manager;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_manager is null)
            {
                var config = new Configuration
                {
                    AppName = "LocalFoundry-Api",
                    LogLevel = Microsoft.AI.Foundry.Local.LogLevel.Information,
                };

                await FoundryLocalManager.CreateAsync(config, _logger, cancellationToken);
                _manager = FoundryLocalManager.Instance;
            }

            return _manager;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
