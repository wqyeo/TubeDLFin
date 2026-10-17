using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Downloads.Naming;
using Jellyfin.Plugin.TubeDLFin.Libraries;

namespace Jellyfin.Plugin.TubeDLFin.Api;

/// <summary>
/// A library as the download form shows it, returned by <c>GET /TubeDLFin/Libraries</c>.
/// </summary>
/// <param name="Id">Jellyfin's id for the library.</param>
/// <param name="Name">The library's name.</param>
/// <param name="Type">The library's type.</param>
/// <param name="DefaultNaming">The naming preset that suits the library's type.</param>
/// <param name="Folders">The library's folders, with free space.</param>
public sealed record LibraryView(string Id, string Name, LibraryType Type, NamingPresetId DefaultNaming, IReadOnlyList<LibraryFolderView> Folders);
