using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace LocalFoundry.Api.Plugins;

/// <summary>
/// Sample Semantic Kernel plugin exposed to the model as a callable tool by the agent endpoint.
/// </summary>
public sealed class TimePlugin
{
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the plugin.</summary>
    /// <param name="timeProvider">Source of the current time and local time zone; injectable so tests can fix the clock.</param>
    public TimePlugin(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>Gets the server's current local date and time.</summary>
    /// <returns>The date and time in the current culture's full date/short time format.</returns>
    [KernelFunction, Description("Gets the current local date and time.")]
    public string GetCurrentTime() => _timeProvider.GetLocalNow().ToString("f");
}
