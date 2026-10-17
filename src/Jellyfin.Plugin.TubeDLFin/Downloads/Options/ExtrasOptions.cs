using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// Thumbnail, tags and chapters. (The <c>.nfo</c> file is written by TubeDLFin itself, see
/// <see cref="Finishing.NfoStep"/>.)
/// </summary>
public sealed class ExtrasOptions : IDownloadOptionSet
{
    /// <inheritdoc />
    public void AddTo(IList<string> arguments, DownloadPlan plan, DownloadLocations locations)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(plan);

        var extras = plan.Profile.Extras;
        if (extras.SaveThumbnail || extras.EmbedThumbnail)
        {
            // Without --write-thumbnail, yt-dlp deletes the thumbnail after embedding it.
            if (extras.SaveThumbnail)
            {
                arguments.Add("--write-thumbnail");
            }

            // Sites serve WebP; JPEG works as a poster everywhere and embeds in MP4, MKV and M4A.
            arguments.Add("--convert-thumbnails");
            arguments.Add("jpg");
        }

        if (extras.EmbedThumbnail)
        {
            arguments.Add("--embed-thumbnail");
        }

        if (extras.EmbedMetadata)
        {
            arguments.Add("--embed-metadata");
        }

        if (extras.EmbedChapters)
        {
            arguments.Add("--embed-chapters");
        }
    }
}
