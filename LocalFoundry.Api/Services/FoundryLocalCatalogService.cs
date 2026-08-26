using LocalFoundry.Api.Models;
using Microsoft.AI.Foundry.Local;

namespace LocalFoundry.Api.Services;

/// <summary>
/// Lazily initializes the Foundry Local native SDK on first use so the app can still start
/// (and serve /api/chat via the REST endpoint) even if Foundry Local isn't installed yet.
/// </summary>
public sealed class FoundryLocalCatalogService
{
    private readonly ILogger<FoundryLocalCatalogService> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private FoundryLocalManager? _manager;

    public FoundryLocalCatalogService(ILogger<FoundryLocalCatalogService> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<FoundryModelSummary>> ListModelsAsync()
    {
        var manager = await EnsureManagerAsync();
        var catalog = await manager.GetCatalogAsync();
        var models = await catalog.ListModelsAsync();
        return models.Select(m => new FoundryModelSummary(m.Alias, m.Id)).ToList();
    }

    private async Task<FoundryLocalManager> EnsureManagerAsync()
    {
        if (_manager is not null)
        {
            return _manager;
        }

        await _initLock.WaitAsync();
        try
        {
            if (_manager is null)
            {
                var config = new Configuration
                {
                    AppName = "LocalFoundry-Api",
                    LogLevel = Microsoft.AI.Foundry.Local.LogLevel.Information,
                };

                await FoundryLocalManager.CreateAsync(config, _logger);
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
