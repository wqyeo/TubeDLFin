using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// What one check of a subscription did.
/// </summary>
/// <param name="CheckedAt">When the check ran.</param>
/// <param name="Error">What went wrong, or <c>null</c>.</param>
/// <param name="QueuedKeys">The archive keys of the uploads queued for download.</param>
public sealed record CheckOutcome(DateTimeOffset CheckedAt, string? Error, IReadOnlyList<string> QueuedKeys)
{
    /// <summary>
    /// Gets a value indicating whether the problem is on this server (yt-dlp not ready) rather
    /// than the site's, so the subscription is checked again on the task's next run instead of
    /// after its interval.
    /// </summary>
    public bool RetryOnNextRun { get; init; }
}
