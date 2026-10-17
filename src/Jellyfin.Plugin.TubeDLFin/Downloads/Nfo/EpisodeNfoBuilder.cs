using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Jellyfin.Plugin.TubeDLFin.Downloads.Output;
using Jellyfin.Plugin.TubeDLFin.Libraries;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Nfo;

/// <summary>
/// <c>&lt;episodedetails&gt;</c> files for shows libraries, plus a <c>tvshow.nfo</c> for the
/// channel's folder so Jellyfin doesn't match the channel to a real TV series.
/// </summary>
/// <remarks>
/// Season and episode come from the upload date and time, the same numbers as the TV-show naming
/// preset puts in file names: season 2026, episode 10131445 for 13 October 14:45 (UTC).
/// Jellyfin merges episodes that share a number, so the time keeps same-day uploads apart.
/// </remarks>
public sealed partial class EpisodeNfoBuilder : INfoBuilder
{
    /// <summary>
    /// Gets the episode number: month, day, hour and minute of the upload (MMDDHHMM).
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The number, 0 if the date is unknown.</returns>
    public static long EpisodeNumber(DownloadedItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var date = item.UploadDate is { } day ? day.ToString("MMdd", CultureInfo.InvariantCulture) : "0000";
        var time = item.Timestamp is { } stamp ? stamp.UtcDateTime.ToString("HHmm", CultureInfo.InvariantCulture) : "0000";
        return long.Parse(date + time, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public bool AppliesTo(LibraryType type) => type == LibraryType.Shows;

    /// <inheritdoc />
    public IReadOnlyList<NfoFile> Build(DownloadedItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var showFolder = ShowFolder(item.FilePath);
        var showTitle = item.Uploader ?? Path.GetFileName(showFolder);

        var episode = new XElement(
            "episodedetails",
            NfoElements.Text("title", item.Title),
            NfoElements.Text("showtitle", showTitle),
            NfoElements.Text("plot", item.Description),
            NfoElements.Date("aired", item.UploadDate),
            NfoElements.Number("season", item.UploadDate?.Year ?? 0),
            NfoElements.Number("episode", EpisodeNumber(item)),
            NfoElements.Runtime(item),
            NfoElements.UniqueId(item),
            NfoElements.LockData());

        var show = new XElement("tvshow", NfoElements.Text("title", showTitle), NfoElements.LockData());

        return
        [
            new NfoFile(NfoElements.PathFor(item.FilePath), NfoElements.Document(episode), Overwrite: true),
            new NfoFile(Path.Combine(showFolder, "tvshow.nfo"), NfoElements.Document(show), Overwrite: false),
        ];
    }

    // The show is the folder holding the "Season N" folders, or the file's own folder.
    private static string ShowFolder(string filePath)
    {
        var folder = Path.GetDirectoryName(filePath) ?? string.Empty;
        return SeasonFolder().IsMatch(Path.GetFileName(folder)) ? Path.GetDirectoryName(folder) ?? folder : folder;
    }

    [GeneratedRegex(@"^Season \d+$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex SeasonFolder();
}
