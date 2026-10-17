using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Jellyfin.Plugin.TubeDLFin.Downloads.Output;
using Jellyfin.Plugin.TubeDLFin.Libraries;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Nfo;

/// <summary>
/// <c>&lt;movie&gt;</c> files for movie, home video and mixed libraries (Jellyfin reads them for
/// plain videos too).
/// </summary>
public sealed class MovieNfoBuilder : INfoBuilder
{
    /// <inheritdoc />
    public bool AppliesTo(LibraryType type) => type is LibraryType.Movies or LibraryType.HomeVideos or LibraryType.Mixed;

    /// <inheritdoc />
    public IReadOnlyList<NfoFile> Build(DownloadedItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var movie = new XElement(
            "movie",
            NfoElements.Text("title", item.Title),
            NfoElements.Text("plot", item.Description),
            NfoElements.Date("premiered", item.UploadDate),
            NfoElements.Number("year", item.UploadDate?.Year),
            NfoElements.Text("studio", item.Uploader),
            NfoElements.Runtime(item),
            NfoElements.UniqueId(item),
            NfoElements.LockData());

        return [new NfoFile(NfoElements.PathFor(item.FilePath), NfoElements.Document(movie), Overwrite: true)];
    }
}
