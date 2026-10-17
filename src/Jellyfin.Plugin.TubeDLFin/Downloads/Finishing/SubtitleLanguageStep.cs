using System;
using System.IO;
using Jellyfin.Plugin.TubeDLFin.Downloads.Output;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Finishing;

/// <summary>
/// Renames YouTube's original-language captions (<c>name.en-orig.srt</c>) to the plain code
/// (<c>name.en.srt</c>): Jellyfin doesn't recognise "en-orig" and lists it as an unknown language.
/// </summary>
public sealed class SubtitleLanguageStep : IFinishedItemStep
{
    private const string OriginalSuffix = "-orig";

    /// <inheritdoc />
    public void Run(DownloadPlan plan, DownloadedItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var folder = Path.GetDirectoryName(item.FilePath);
        if (folder is null || !Directory.Exists(folder))
        {
            return;
        }

        var stem = Path.GetFileNameWithoutExtension(item.FilePath);
        foreach (var file in Directory.EnumerateFiles(folder, $"{EscapeWildcards(stem)}.*{OriginalSuffix}.srt"))
        {
            var name = Path.GetFileName(file);
            if (!name.StartsWith(stem + ".", StringComparison.Ordinal))
            {
                continue; // The pattern's wildcards matched another file's name.
            }

            // "name.en-orig.srt" → language "en".
            var language = name[(stem.Length + 1)..^(OriginalSuffix.Length + ".srt".Length)];
            var target = Path.Combine(folder, $"{stem}.{language}.srt");
            if (language.Length > 0 && !File.Exists(target))
            {
                File.Move(file, target);
            }
        }
    }

    // File names can contain "[" and "]" (yt-dlp's "[id]"); only * and ? are wildcards for
    // EnumerateFiles, so replace those with a single-character wildcard and check the name after.
    private static string EscapeWildcards(string name) => name.Replace('*', '?');
}
