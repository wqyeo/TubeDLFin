using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Downloads.Output;
using Jellyfin.Plugin.TubeDLFin.Libraries;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Nfo;

/// <summary>
/// Builds the <c>.nfo</c> files Jellyfin reads for one kind of library. Register one per
/// library type; music has none (Jellyfin reads music from the files' tags).
/// </summary>
public interface INfoBuilder
{
    /// <summary>
    /// Tells whether this builder handles a library type.
    /// </summary>
    /// <param name="type">The library type.</param>
    /// <returns><c>true</c> if it does.</returns>
    bool AppliesTo(LibraryType type);

    /// <summary>
    /// Builds the files for a finished item.
    /// </summary>
    /// <param name="item">The file and its metadata.</param>
    /// <returns>The files to write.</returns>
    IReadOnlyList<NfoFile> Build(DownloadedItem item);
}
