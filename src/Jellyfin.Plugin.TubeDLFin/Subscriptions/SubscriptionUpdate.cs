using Jellyfin.Plugin.TubeDLFin.Downloads;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// The body of <c>PUT /TubeDLFin/Subscriptions/{id}</c>. Values left out (<c>null</c>) are kept.
/// </summary>
public sealed record SubscriptionUpdate
{
    /// <summary>
    /// Gets the new name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the new hours between checks.
    /// </summary>
    public int? IntervalHours { get; init; }

    /// <summary>
    /// Gets the new download options.
    /// </summary>
    public DownloadProfile? Profile { get; init; }
}
