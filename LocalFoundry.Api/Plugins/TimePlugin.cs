using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace LocalFoundry.Api.Plugins;

public sealed class TimePlugin
{
    [KernelFunction, Description("Gets the current local date and time.")]
    public string GetCurrentTime() => DateTimeOffset.Now.ToString("f");
}
