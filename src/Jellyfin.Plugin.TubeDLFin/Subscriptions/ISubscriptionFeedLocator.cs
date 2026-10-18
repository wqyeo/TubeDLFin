using Jellyfin.Plugin.TubeDLFin.YtDlp;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// Finds the feed for any link, asking each site's <see cref="ISubscriptionFeedResolver"/>.
/// </summary>
public interface ISubscriptionFeedLocator
{
    /// <summary>
    /// Finds the feed for a link.
    /// </summary>
    /// <param name="url">The link the admin entered.</param>
    /// <returns>The feed.</returns>
    SubscriptionFeed Locate(MediaUrl url);
}
