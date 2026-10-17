using System;
using System.Collections.Generic;
using System.Globalization;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// Format selection for video downloads: quality, codec preference and container.
/// </summary>
public sealed class VideoFormatOptions : IDownloadOptionSet
{
    /// <inheritdoc />
    public void AddTo(IList<string> arguments, DownloadPlan plan, DownloadLocations locations)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(plan);

        var profile = plan.Profile;
        if (profile.Mode != DownloadMode.Video)
        {
            return;
        }

        arguments.Add("--format");
        arguments.Add(Selector(plan.FormatId, profile.MaxHeight));

        // Sort order (-S) only breaks ties within the selector: preferred codecs and containers.
        var sort = new List<string>();
        if (profile.PreferH264)
        {
            sort.Add("vcodec:h264");
            sort.Add("acodec:aac");
        }

        if (profile.Container == VideoContainer.Webm)
        {
            sort.Add("vext:webm");
            sort.Add("aext:webm");
        }

        if (sort.Count > 0)
        {
            arguments.Add("--format-sort");
            arguments.Add(string.Join(',', sort));
        }

        if (profile.Container != VideoContainer.Auto)
        {
            var container = profile.Container switch
            {
                VideoContainer.Mp4 => "mp4",
                VideoContainer.Mkv => "mkv",
                _ => "webm",
            };
            arguments.Add("--merge-output-format");
            arguments.Add(container);
            arguments.Add("--remux-video");
            arguments.Add(container);
        }
    }

    // A chosen format that has no audio gets the best audio added; "[acodec=none]" makes the
    // first choice fail for formats that already have audio, so they're used as they are.
    // Otherwise: the best video (up to the height) plus the best audio, falling back to the
    // best single file, and finally to anything if nothing is that small.
    private static string Selector(string? formatId, int? maxHeight)
    {
        if (formatId is not null)
        {
            return $"{formatId}[acodec=none]+ba/{formatId}";
        }

        if (maxHeight is { } height)
        {
            var limit = string.Create(CultureInfo.InvariantCulture, $"[height<={height}]");
            return $"bv*{limit}+ba/b{limit}/bv*+ba/b";
        }

        return "bv*+ba/b";
    }
}
