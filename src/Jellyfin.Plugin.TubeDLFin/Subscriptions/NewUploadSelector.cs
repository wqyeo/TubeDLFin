using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// Decides which videos of a feed a check queues. Pure: no I/O.
/// </summary>
public static class NewUploadSelector
{
    /// <summary>
    /// The most videos one check queues, so a busy channel can't flood the queue. The rest are
    /// queued by the next checks.
    /// </summary>
    public const int MaxPerCheck = 10;

    /// <summary>
    /// Picks the new uploads: listed now, but not when the admin subscribed, not downloaded into
    /// the library yet, not waiting in the queue, and not given up on. The oldest come first.
    /// </summary>
    /// <param name="listing">The feed's videos, in the site's order.</param>
    /// <param name="subscription">The subscription.</param>
    /// <param name="archive">The library's download archive.</param>
    /// <param name="queuedUrls">Links of this subscription's unfinished downloads.</param>
    /// <returns>The videos to queue, at most <see cref="MaxPerCheck"/>.</returns>
    public static IReadOnlyList<FeedEntry> Select(
        IReadOnlyList<FeedEntry> listing,
        Subscription subscription,
        IReadOnlySet<string> archive,
        IReadOnlySet<string> queuedUrls)
    {
        ArgumentNullException.ThrowIfNull(listing);
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentNullException.ThrowIfNull(queuedUrls);

        var baseline = subscription.Baseline.ToHashSet(StringComparer.Ordinal);
        var candidates = listing
            .DistinctBy(entry => entry.Key, StringComparer.Ordinal)
            .Where(entry => !baseline.Contains(entry.Key)
                && !archive.Contains(entry.Key)
                && !queuedUrls.Contains(entry.Url.AbsoluteUri)
                && subscription.Attempts.GetValueOrDefault(entry.Key) < Subscription.MaxAttempts);

        return (subscription.NewestFirst ? candidates.Reverse() : candidates).Take(MaxPerCheck).ToList();
    }
}
