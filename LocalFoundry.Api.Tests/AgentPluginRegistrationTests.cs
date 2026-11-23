using LocalFoundry.Api.Plugins;
using LocalFoundry.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace LocalFoundry.Api.Tests;

/// <summary>Checks the real DI wiring: the app's kernel offers the model every agent tool.</summary>
public class AgentPluginRegistrationTests
{
    [Fact]
    public void AppKernel_OffersTimeAndMathFunctions()
    {
        // WebApplicationFactory<Program> directly: LocalFoundryApiFactory swaps in fakes, and
        // this test is about the real registrations.
        using var factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>();
        var kernel = factory.Services.GetRequiredService<IKernelProvider>().GetKernel(null);

        var functions = kernel.Plugins.GetFunctionsMetadata().Select(f => $"{f.PluginName}.{f.Name}").Order();

        Assert.Equal(
            [
                $"{nameof(MathPlugin)}.Add", $"{nameof(MathPlugin)}.Divide", $"{nameof(MathPlugin)}.Multiply",
                $"{nameof(MathPlugin)}.Subtract", $"{nameof(TimePlugin)}.GetCurrentTime",
            ],
            functions);
    }
}
