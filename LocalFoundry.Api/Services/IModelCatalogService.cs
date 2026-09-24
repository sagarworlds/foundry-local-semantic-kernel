using LocalFoundry.Api.Models;

namespace LocalFoundry.Api.Services;

/// <summary>
/// Lists the models available to the local inference runtime.
/// </summary>
public interface IModelCatalogService
{
    /// <summary>
    /// Returns every model in the local catalog.
    /// </summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>A summary (alias and id) of each available model.</returns>
    Task<IReadOnlyList<FoundryModelSummary>> ListModelsAsync(CancellationToken cancellationToken);
}
