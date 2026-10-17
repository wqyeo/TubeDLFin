using System;
using System.Collections.Generic;
using System.Globalization;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// Format selection for audio-only downloads: the best audio, extracted and optionally converted.
/// </summary>
public sealed class AudioFormatOptions : IDownloadOptionSet
{
    /// <inheritdoc />
    public void AddTo(IList<string> arguments, DownloadPlan plan, DownloadLocations locations)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(plan);

        var profile = plan.Profile;
        if (profile.Mode != DownloadMode.Audio)
        {
            return;
        }

        arguments.Add("--format");

        // For M4A, YouTube's AAC stream fits as it is: prefer it, so nothing is re-encoded.
        arguments.Add(plan.FormatId ?? (profile.AudioFormat == AudioFormat.M4a ? "ba[ext=m4a]/ba/b" : "ba/b"));
        arguments.Add("--extract-audio");

        if (profile.AudioFormat != AudioFormat.Original)
        {
            arguments.Add("--audio-format");
            arguments.Add(profile.AudioFormat switch
            {
                AudioFormat.M4a => "m4a",
                AudioFormat.Mp3 => "mp3",
                AudioFormat.Opus => "opus",
                AudioFormat.Flac => "flac",
                _ => "wav",
            });
        }

        // A bitrate only means something when re-encoding to a lossy format.
        if (profile.AudioQuality is { } kbps && profile.AudioFormat is AudioFormat.M4a or AudioFormat.Mp3 or AudioFormat.Opus)
        {
            arguments.Add("--audio-quality");
            arguments.Add(string.Create(CultureInfo.InvariantCulture, $"{kbps}K"));
        }
    }
}
