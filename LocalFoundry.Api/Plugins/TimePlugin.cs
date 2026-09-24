using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace LocalFoundry.Api.Plugins;

/// <summary>
/// Sample Semantic Kernel plugin exposed to the model as a callable tool by the agent endpoint.
/// </summary>
public sealed class TimePlugin
{
    /// <summary>Gets the server's current local date and time.</summary>
    /// <returns>The date and time in the current culture's full date/short time format.</returns>
    [KernelFunction, Description("Gets the current local date and time.")]
    public string GetCurrentTime() => DateTimeOffset.Now.ToString("f");
}
