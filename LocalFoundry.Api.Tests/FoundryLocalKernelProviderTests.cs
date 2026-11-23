using LocalFoundry.Api.Options;
using LocalFoundry.Api.Plugins;
using LocalFoundry.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Services;

namespace LocalFoundry.Api.Tests;

public sealed class FoundryLocalKernelProviderTests : IDisposable
{
    private readonly ServiceProvider _services = new ServiceCollection().AddHttpClient().BuildServiceProvider();
    private readonly FoundryLocalKernelProvider _provider;

    public FoundryLocalKernelProviderTests()
    {
        _provider = new FoundryLocalKernelProvider(
            Microsoft.Extensions.Options.Options.Create(new FoundryLocalOptions { ModelId = "default-model" }),
            _services.GetRequiredService<IHttpClientFactory>(),
            [KernelPluginFactory.CreateFromObject(new MathPlugin(), "First"), KernelPluginFactory.CreateFromObject(new MathPlugin(), "Second")]);
    }

    public void Dispose() => _services.Dispose();

    [Theory]
    [InlineData(null)]
    [InlineData("default-model")]
    [InlineData("DEFAULT-MODEL")]
    public void GetKernel_DefaultModel_ReturnsTheSameCachedKernel(string? modelId)
    {
        var kernel = _provider.GetKernel(modelId);

        Assert.Same(_provider.GetKernel(null), kernel);
        Assert.Equal("default-model", ModelIdOf(kernel));
    }

    [Fact]
    public void GetKernel_OtherModel_ReturnsKernelBoundToThatModel()
    {
        var kernel = _provider.GetKernel("phi-4-mini");

        Assert.NotSame(_provider.GetKernel(null), kernel);
        Assert.Equal("phi-4-mini", ModelIdOf(kernel));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("phi-4-mini")]
    public void GetKernel_AnyModel_RegistersEveryPluginItWasGiven(string? modelId)
    {
        Assert.Equal(["First", "Second"], _provider.GetKernel(modelId).Plugins.Select(plugin => plugin.Name).Order());
    }

    private static string? ModelIdOf(Kernel kernel) => kernel.GetRequiredService<IChatCompletionService>().GetModelId();
}
