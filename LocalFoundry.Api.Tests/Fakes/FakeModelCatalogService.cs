using LocalFoundry.Api.Models;
using LocalFoundry.Api.Services;

namespace LocalFoundry.Api.Tests.Fakes;

/// <summary>Scriptable <see cref="IModelCatalogService"/> returning a fixed list or throwing.</summary>
public sealed class FakeModelCatalogService : IModelCatalogService
{
    public IReadOnlyList<FoundryModelSummary> Models { get; set; } =
        [new FoundryModelSummary("qwen2.5-0.5b", "qwen2.5-0.5b-instruct-generic-cpu:4")];

    public Exception? ExceptionToThrow { get; set; }

    public Task<IReadOnlyList<FoundryModelSummary>> ListModelsAsync(CancellationToken cancellationToken) =>
        ExceptionToThrow is null ? Task.FromResult(Models) : Task.FromException<IReadOnlyList<FoundryModelSummary>>(ExceptionToThrow);
}
