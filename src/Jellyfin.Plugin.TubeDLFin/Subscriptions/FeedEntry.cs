using System;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// One video of a subscription's feed.
/// </summary>
/// <param name="Key">The download archive key, e.g. <c>youtube aqz-KE-bpKQ</c>.</param>
/// <param name="Title">The title.</param>
/// <param name="Url">The video's page.</param>
public sealed record FeedEntry(string Key, string Title, Uri Url);
