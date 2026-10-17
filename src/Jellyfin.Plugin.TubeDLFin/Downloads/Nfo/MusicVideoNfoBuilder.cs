using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Jellyfin.Plugin.TubeDLFin.Downloads.Output;
using Jellyfin.Plugin.TubeDLFin.Libraries;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Nfo;

/// <summary>
/// <c>&lt;musicvideo&gt;</c> files for music video libraries.
/// </summary>
public sealed class MusicVideoNfoBuilder : INfoBuilder
{
    /// <inheritdoc />
    public bool AppliesTo(LibraryType type) => type == LibraryType.MusicVideos;

    /// <inheritdoc />
    public IReadOnlyList<NfoFile> Build(DownloadedItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var video = new XElement(
            "musicvideo",
            NfoElements.Text("title", item.Track ?? item.Title),
            NfoElements.Text("artist", item.Artist ?? item.Uploader),
            NfoElements.Text("album", item.Album),
            NfoElements.Text("plot", item.Description),
            NfoElements.Date("premiered", item.UploadDate),
            NfoElements.Number("year", item.ReleaseYear ?? item.UploadDate?.Year),
            NfoElements.Runtime(item),
            NfoElements.UniqueId(item),
            NfoElements.LockData());

        return [new NfoFile(NfoElements.PathFor(item.FilePath), NfoElements.Document(video), Overwrite: true)];
    }
}
