using System;
using Jellyfin.Plugin.TubeDLFin.Downloads;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// A subscription as the page shows it.
/// </summary>
public sealed record SubscriptionInfo
{
    /// <summary>
    /// Gets the subscription's id.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the link checked for new uploads.
    /// </summary>
    public required Uri Url { get; init; }

    /// <summary>
    /// Gets the name of the library downloaded into, or <c>null</c> if it no longer exists.
    /// </summary>
    public string? LibraryName { get; init; }

    /// <summary>
    /// Gets the hours between checks.
    /// </summary>
    public required int IntervalHours { get; init; }

    /// <summary>
    /// Gets a value indicating whether checks are paused.
    /// </summary>
    public required bool Paused { get; init; }

    /// <summary>
    /// Gets when the admin subscribed.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets when the last check ran.
    /// </summary>
    public DateTimeOffset? LastCheckedAt { get; init; }

    /// <summary>
    /// Gets when the next check is due.
    /// </summary>
    public required DateTimeOffset NextCheckAt { get; init; }

    /// <summary>
    /// Gets what went wrong in the last check.
    /// </summary>
    public string? LastError { get; init; }

    /// <summary>
    /// Gets how many new uploads were downloaded.
    /// </summary>
    public required int Downloaded { get; init; }

    /// <summary>
    /// Gets how many new uploads are waiting in the queue or downloading.
    /// </summary>
    public required int Queued { get; init; }

    /// <summary>
    /// Gets how many new uploads failed <see cref="Subscription.MaxAttempts"/> times and won't be tried again.
    /// </summary>
    public required int GivenUp { get; init; }

    /// <summary>
    /// Gets how new uploads are downloaded.
    /// </summary>
    public required DownloadProfile Profile { get; init; }
}
