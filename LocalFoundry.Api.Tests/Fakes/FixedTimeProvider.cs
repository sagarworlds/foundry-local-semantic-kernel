namespace LocalFoundry.Api.Tests.Fakes;

/// <summary>A clock frozen at one instant in one time zone.</summary>
public sealed class FixedTimeProvider(DateTimeOffset utcNow, TimeZoneInfo localTimeZone) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;

    public override TimeZoneInfo LocalTimeZone => localTimeZone;
}
