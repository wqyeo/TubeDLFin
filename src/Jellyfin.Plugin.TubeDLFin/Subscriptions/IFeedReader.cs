using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// Lists a subscription's feed.
/// </summary>
public interface IFeedReader
{
    /// <summary>
    /// Lists the videos of a feed without downloading anything.
    /// </summary>
    /// <param name="feed">The feed.</param>
    /// <param name="cancellationToken">Stops yt-dlp.</param>
    /// <returns>The videos and a name for the subscription.</returns>
    /// <exception cref="Detection.ProbeFailedException">The site refused the link, it isn't a list of videos, or yt-dlp failed.</exception>
    /// <exception cref="Tools.ToolNotReadyException">yt-dlp is not installed or not working.</exception>
    Task<FeedListing> ReadAsync(SubscriptionFeed feed, CancellationToken cancellationToken);
}
