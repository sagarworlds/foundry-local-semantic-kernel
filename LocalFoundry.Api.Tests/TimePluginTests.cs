using System.Globalization;
using LocalFoundry.Api.Plugins;
using LocalFoundry.Api.Tests.Fakes;

namespace LocalFoundry.Api.Tests;

public class TimePluginTests
{
    [Fact]
    public void GetCurrentTime_ReturnsTheProvidersLocalTime()
    {
        // 23:30 UTC on the 14th is already 04:30 on the 15th at UTC+5, which proves the plugin
        // reports local time rather than UTC.
        var utcNow = new DateTimeOffset(2026, 3, 14, 23, 30, 0, TimeSpan.Zero);
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(5), "Test", "Test");
        var plugin = new TimePlugin(new FixedTimeProvider(utcNow, zone));

        var previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            Assert.Equal("Sunday, 15 March 2026 04:30", plugin.GetCurrentTime());
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }
}
