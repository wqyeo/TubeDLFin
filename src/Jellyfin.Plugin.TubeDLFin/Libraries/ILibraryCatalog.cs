using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Libraries;

/// <summary>
/// Lists the libraries TubeDLFin can download into.
/// </summary>
public interface ILibraryCatalog
{
    /// <summary>
    /// Gets the video and music libraries that have at least one folder, sorted by name.
    /// </summary>
    /// <returns>The libraries.</returns>
    IReadOnlyList<LibraryInfo> GetLibraries();
}
