using System.Diagnostics.CodeAnalysis;
using Jellyfin.Plugin.TubeDLFin.Downloads;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// The body of <c>POST /TubeDLFin/Subscriptions</c>.
/// </summary>
public sealed record SubscriptionRequest
{
    /// <summary>
    /// Gets the channel or playlist link.
    /// </summary>
    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Checked by MediaUrl, which explains what is wrong.")]
    public string? Url { get; init; }

    /// <summary>
    /// Gets the name to show, or <c>null</c> for the channel's or playlist's own.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the hours between checks, or <c>null</c> for <see cref="Subscription.DefaultIntervalHours"/>.
    /// </summary>
    public int? IntervalHours { get; init; }

    /// <summary>
    /// Gets how new uploads are downloaded.
    /// </summary>
    public DownloadProfile? Profile { get; init; }
}
