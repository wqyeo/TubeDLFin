using System;
using System.IO;
using Jellyfin.Plugin.TubeDLFin.Downloads.Output;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Finishing;

/// <summary>
/// Renames YouTube's original-language captions (<c>name.en-orig.srt</c>), which Jellyfin lists
/// as an unknown language: to <c>name.en.srt</c>, or to <c>name.en.orig.srt</c> (shown as
/// "orig - English") when there already is an English file.
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
            if (language.Length == 0)
            {
                continue;
            }

            var plain = Path.Combine(folder, $"{stem}.{language}.srt");
            var labelled = Path.Combine(folder, $"{stem}.{language}.orig.srt");
            if (!File.Exists(plain))
            {
                File.Move(file, plain);
            }
            else if (!File.Exists(labelled))
            {
                File.Move(file, labelled);
            }
        }
    }

    // File names can contain "[" and "]" (yt-dlp's "[id]"); only * and ? are wildcards for
    // EnumerateFiles, so replace those with a single-character wildcard and check the name after.
    private static string EscapeWildcards(string name) => name.Replace('*', '?');
}
