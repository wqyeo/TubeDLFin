using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// Subtitles as <c>.srt</c> files next to the video (<c>name.en.srt</c>, which Jellyfin finds),
/// optionally also embedded.
/// </summary>
public sealed class SubtitleOptions : IDownloadOptionSet
{
    /// <inheritdoc />
    public void AddTo(IList<string> arguments, DownloadPlan plan, DownloadLocations locations)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(plan);

        var subtitles = plan.Profile.Subtitles;
        var languages = subtitles?.Languages?.Replace(" ", string.Empty, StringComparison.Ordinal);
        if (subtitles is null || string.IsNullOrEmpty(languages))
        {
            return;
        }

        // An explicit --write-subs also keeps the files when they are embedded.
        arguments.Add("--write-subs");
        if (subtitles.IncludeAutomatic)
        {
            arguments.Add("--write-auto-subs");
        }

        arguments.Add("--sub-langs");
        arguments.Add(languages);
        arguments.Add("--convert-subs");
        arguments.Add("srt");

        if (subtitles.Embed && plan.Profile.Mode == DownloadMode.Video)
        {
            arguments.Add("--embed-subs");
        }
    }
}
