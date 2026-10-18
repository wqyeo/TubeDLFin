using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.TubeDLFin.Downloads;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// A channel or playlist checked on a schedule, whose new uploads are downloaded with a saved
/// profile. Saved as JSON (see <see cref="ISubscriptionStore"/>), so properties are only added,
/// never renamed.
/// </summary>
public sealed record Subscription
{
    /// <summary>
    /// The shortest check interval, in hours.
    /// </summary>
    public const int MinIntervalHours = 1;

    /// <summary>
    /// The longest check interval, in hours (a week).
    /// </summary>
    public const int MaxIntervalHours = 168;

    /// <summary>
    /// The check interval used unless the admin picks another.
    /// </summary>
    public const int DefaultIntervalHours = 6;

    /// <summary>
    /// How often a new upload is queued before TubeDLFin gives up on it.
    /// </summary>
    public const int MaxAttempts = 3;

    /// <summary>
    /// Gets the subscription's id.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the name shown on the page.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the link that is listed on each check, e.g. a channel's Videos tab.
    /// </summary>
    public required Uri Url { get; init; }

    /// <summary>
    /// Gets how many items of <see cref="Url"/> are listed on each check.
    /// </summary>
    public required int ListLimit { get; init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Url"/> lists the newest items first.
    /// </summary>
    public bool NewestFirst { get; init; }

    /// <summary>
    /// Gets how new uploads are downloaded, and into which library.
    /// </summary>
    public required DownloadProfile Profile { get; init; }

    /// <summary>
    /// Gets how many hours pass between checks.
    /// </summary>
    public int IntervalHours { get; init; } = DefaultIntervalHours;

    /// <summary>
    /// Gets a value indicating whether checks are paused.
    /// </summary>
    public bool Paused { get; init; }

    /// <summary>
    /// Gets when the admin subscribed.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets when the last check ran, or <c>null</c> before the first.
    /// </summary>
    public DateTimeOffset? LastCheckedAt { get; init; }

    /// <summary>
    /// Gets when the next check is due.
    /// </summary>
    public required DateTimeOffset NextCheckAt { get; init; }

    /// <summary>
    /// Gets what went wrong in the last check, or <c>null</c> if it went well.
    /// </summary>
    public string? LastError { get; init; }

    /// <summary>
    /// Gets the archive keys of the items listed when the admin subscribed. They are not new
    /// uploads, so they are never downloaded.
    /// </summary>
    public IReadOnlyList<string> Baseline { get; init; } = [];

    /// <summary>
    /// Gets the archive keys of the items this subscription queued, with how often each was queued.
    /// </summary>
    public IReadOnlyDictionary<string, int> Attempts { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// Checks whether a check is due.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <returns><c>true</c> unless paused or checked recently.</returns>
    public bool IsDue(DateTimeOffset now) => !Paused && NextCheckAt <= now;

    /// <summary>
    /// Records a check's outcome.
    /// </summary>
    /// <param name="outcome">What the check found.</param>
    /// <returns>The subscription after the check.</returns>
    public Subscription After(CheckOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        var attempts = new Dictionary<string, int>(Attempts, StringComparer.Ordinal);
        foreach (var key in outcome.QueuedKeys)
        {
            attempts[key] = attempts.GetValueOrDefault(key) + 1;
        }

        return this with
        {
            LastCheckedAt = outcome.CheckedAt,
            LastError = outcome.Error,
            NextCheckAt = outcome.RetryOnNextRun ? outcome.CheckedAt : outcome.CheckedAt.AddHours(IntervalHours),
            Attempts = attempts,
        };
    }

    /// <summary>
    /// Counts the items this subscription queued that are now in the library's archive.
    /// </summary>
    /// <param name="archive">The library's download archive.</param>
    /// <returns>How many new uploads were downloaded.</returns>
    public int CountDownloaded(IReadOnlySet<string> archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        return Attempts.Keys.Count(archive.Contains);
    }

    /// <summary>
    /// Counts the items TubeDLFin gave up on after <see cref="MaxAttempts"/> tries.
    /// </summary>
    /// <param name="archive">The library's download archive.</param>
    /// <returns>How many new uploads failed for good.</returns>
    public int CountGivenUp(IReadOnlySet<string> archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        return Attempts.Count(a => a.Value >= MaxAttempts && !archive.Contains(a.Key));
    }
}
