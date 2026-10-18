using Jellyfin.Plugin.TubeDLFin.YtDlp;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// What a subscription lists on each check.
/// </summary>
/// <param name="Url">The link to list.</param>
/// <param name="ListLimit">How many items to list, from the first.</param>
/// <param name="NewestFirst">Whether the link lists the newest items first.</param>
public sealed record SubscriptionFeed(MediaUrl Url, int ListLimit, bool NewestFirst);
