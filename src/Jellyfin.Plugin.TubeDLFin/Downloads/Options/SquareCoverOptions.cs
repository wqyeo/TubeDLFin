using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// Crops embedded cover art to a centred square. Video thumbnails are 16:9 and look letterboxed
/// as album art; square covers (YouTube Music albums) are left as they are.
/// </summary>
public sealed class SquareCoverOptions : IDownloadOptionSet
{
    /// <summary>
    /// The FFmpeg arguments for yt-dlp's thumbnail converter: crop to the shorter side.
    /// </summary>
    public const string CropArguments = "ThumbnailsConvertor+FFmpeg_o:-c:v mjpeg -vf crop=\"'if(gt(ih,iw),iw,ih)':'if(gt(iw,ih),ih,iw)'\"";

    /// <inheritdoc />
    public void AddTo(IList<string> arguments, DownloadPlan plan, DownloadLocations locations)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(plan);

        var profile = plan.Profile;
        if (profile.Music is { Enabled: true, SquareCover: true } && profile.Extras.EmbedThumbnail)
        {
            arguments.Add("--postprocessor-args");
            arguments.Add(CropArguments);
        }
    }
}
