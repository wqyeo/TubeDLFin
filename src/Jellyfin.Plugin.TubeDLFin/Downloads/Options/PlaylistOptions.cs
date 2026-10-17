using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// Which items of a playlist to download.
/// </summary>
public sealed class PlaylistOptions : IDownloadOptionSet
{
    /// <inheritdoc />
    public void AddTo(IList<string> arguments, DownloadPlan plan, DownloadLocations locations)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.NoPlaylist)
        {
            arguments.Add("--no-playlist");
        }

        if (plan.PlaylistItems is not null)
        {
            arguments.Add("--playlist-items");
            arguments.Add(plan.PlaylistItems);
        }
    }
}
