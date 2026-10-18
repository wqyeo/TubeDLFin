using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jellyfin.Plugin.TubeDLFin.YtDlp;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// <see cref="ISubscriptionFeedResolver"/> for YouTube. A channel's main page lists its tabs
/// (Videos, Live, Shorts) rather than videos, so it becomes the Videos tab. Channel tabs list
/// the newest videos first; playlists, including <c>watch?v=…&amp;list=…</c> links, are listed in full.
/// </summary>
public sealed class YouTubeFeedResolver : ISubscriptionFeedResolver
{
    /// <summary>
    /// How many videos of a channel tab are listed: enough for a busy channel between checks.
    /// </summary>
    public const int ChannelListLimit = 50;

    private const string Origin = "https://www.youtube.com";

    private static readonly HashSet<string> _hosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "youtube.com",
        "www.youtube.com",
        "m.youtube.com",
    };

    private static readonly HashSet<string> _videoTabs = new(StringComparer.OrdinalIgnoreCase) { "videos", "streams", "shorts" };

    /// <inheritdoc />
    public SubscriptionFeed? Resolve(MediaUrl url)
    {
        ArgumentNullException.ThrowIfNull(url);

        var uri = url.Value;
        if (!_hosts.Contains(uri.Host))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var list = HttpUtility.ParseQueryString(uri.Query)["list"];
        if (segments is ["playlist" or "watch"] && !string.IsNullOrEmpty(list))
        {
            return Feed($"{Origin}/playlist?list={Uri.EscapeDataString(list)}", SubscriptionFeedLocator.DefaultListLimit, newestFirst: false);
        }

        var channelLength = segments switch
        {
            [var handle, ..] when handle.StartsWith('@') => 1,
            ["channel" or "c" or "user", _, ..] => 2,
            _ => 0,
        };
        if (channelLength == 0)
        {
            return null;
        }

        var tab = segments.Length > channelLength ? segments[channelLength] : "featured";
        if (tab.Equals("featured", StringComparison.OrdinalIgnoreCase))
        {
            tab = "videos";
        }
        else if (!_videoTabs.TryGetValue(tab, out tab))
        {
            return null; // Playlists, posts, ...: not a list of videos.
        }

        var channel = string.Join('/', segments.Take(channelLength));
        return Feed($"{Origin}/{channel}/{tab}", ChannelListLimit, newestFirst: true);
    }

    private static SubscriptionFeed? Feed(string link, int listLimit, bool newestFirst) =>
        MediaUrl.TryCreate(link, out var url, out _) ? new SubscriptionFeed(url, listLimit, newestFirst) : null;
}
