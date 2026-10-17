using System;

namespace Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;

/// <summary>
/// A clock that only moves when told to.
/// </summary>
internal sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
