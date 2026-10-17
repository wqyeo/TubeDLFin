using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// SponsorBlock settings for a download (YouTube only).
/// </summary>
public sealed record SponsorBlockChoice
{
    /// <summary>
    /// Gets what to do with the segments.
    /// </summary>
    public SponsorBlockMode Mode { get; init; }

    /// <summary>
    /// Gets the categories to act on.
    /// </summary>
    public IReadOnlyList<SponsorBlockCategory> Categories { get; init; } = [];
}
