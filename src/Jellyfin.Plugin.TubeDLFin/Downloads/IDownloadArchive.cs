using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// The download archives: one file per library listing everything downloaded into it, one
/// <c>extractor id</c> line per item (see <see cref="DownloadArchiveKeys"/>). yt-dlp reads and
/// writes them through <c>--download-archive</c>; subscriptions read them to find new uploads.
/// </summary>
public interface IDownloadArchive
{
    /// <summary>
    /// Gets a library's archive file and creates its folder. The file itself may not exist yet.
    /// </summary>
    /// <param name="libraryId">Jellyfin's id of the library.</param>
    /// <returns>The full path of the file.</returns>
    string PrepareFile(string libraryId);

    /// <summary>
    /// Reads a library's archive.
    /// </summary>
    /// <param name="libraryId">Jellyfin's id of the library.</param>
    /// <returns>The archive keys, or none if nothing was downloaded into the library yet.</returns>
    IReadOnlySet<string> Read(string libraryId);
}
