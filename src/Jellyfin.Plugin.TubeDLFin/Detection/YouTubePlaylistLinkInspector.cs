using System;
using System.Collections.Generic;
using System.Web;
using Jellyfin.Plugin.TubeDLFin.YtDlp;

namespace Jellyfin.Plugin.TubeDLFin.Detection;

/// <summary>
/// <see cref="IPlaylistLinkInspector"/> for YouTube and YouTube Music:
/// <c>/watch?v=ID&amp;list=PL…</c> and <c>youtu.be/ID?list=PL…</c>.
/// </summary>
public sealed class YouTubePlaylistLinkInspector : IPlaylistLinkInspector
{
    private static readonly HashSet<string> _watchHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "youtube.com",
        "www.youtube.com",
        "m.youtube.com",
        "music.youtube.com",
    };

    /// <inheritdoc />
    public bool HasPlaylistChoice(MediaUrl url)
    {
        ArgumentNullException.ThrowIfNull(url);

        var uri = url.Value;
        var query = HttpUtility.ParseQueryString(uri.Query);
        if (string.IsNullOrEmpty(query["list"]))
        {
            return false;
        }

        if (_watchHosts.Contains(uri.Host))
        {
            return uri.AbsolutePath.Equals("/watch", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(query["v"]);
        }

        return uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase) && uri.AbsolutePath.Trim('/').Length > 0;
    }
}
