using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Libraries;

/// <summary>
/// A Jellyfin library TubeDLFin can download into.
/// </summary>
/// <param name="Id">Jellyfin's id for the library.</param>
/// <param name="Name">The library's name.</param>
/// <param name="Type">The library's type.</param>
/// <param name="Folders">The library's folders on the server.</param>
public sealed record LibraryInfo(string Id, string Name, LibraryType Type, IReadOnlyList<string> Folders);
