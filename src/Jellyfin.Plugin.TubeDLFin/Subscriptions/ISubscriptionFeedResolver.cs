using Jellyfin.Plugin.TubeDLFin.YtDlp;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// Turns a link the admin subscribes to into the link checked for new uploads, for one site.
/// Register one per site; links no resolver knows are listed as they are.
/// </summary>
public interface ISubscriptionFeedResolver
{
    /// <summary>
    /// Resolves a link.
    /// </summary>
    /// <param name="url">The link the admin entered.</param>
    /// <returns>The feed, or <c>null</c> if the link isn't one this resolver knows.</returns>
    SubscriptionFeed? Resolve(MediaUrl url);
}
