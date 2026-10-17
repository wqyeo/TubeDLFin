using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.TubeDLFin.Libraries;

/// <summary>
/// <see cref="ILibraryCatalog"/> reading Jellyfin's libraries ("virtual folders").
/// </summary>
public sealed class JellyfinLibraryCatalog : ILibraryCatalog
{
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinLibraryCatalog"/> class.
    /// </summary>
    /// <param name="libraryManager">Jellyfin's library manager.</param>
    public JellyfinLibraryCatalog(ILibraryManager libraryManager)
    {
        _libraryManager = libraryManager ?? throw new ArgumentNullException(nameof(libraryManager));
    }

    /// <inheritdoc />
    public IReadOnlyList<LibraryInfo> GetLibraries() =>
        _libraryManager.GetVirtualFolders()
            .Select(ToLibrary)
            .OfType<LibraryInfo>()
            .OrderBy(library => library.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private static LibraryInfo? ToLibrary(VirtualFolderInfo folder)
    {
        var type = ToType(folder.CollectionType);
        var folders = folder.Locations?.Where(location => !string.IsNullOrWhiteSpace(location)).ToList() ?? [];
        if (type is null || folders.Count == 0 || string.IsNullOrEmpty(folder.ItemId))
        {
            return null;
        }

        return new LibraryInfo(folder.ItemId, folder.Name ?? folder.ItemId, type.Value, folders);
    }

    // Books, playlists, collections and live TV aren't download targets. A library without a
    // type is Jellyfin's "Mixed Movies and Shows".
    private static LibraryType? ToType(CollectionTypeOptions? type) => type switch
    {
        null or CollectionTypeOptions.mixed => LibraryType.Mixed,
        CollectionTypeOptions.movies => LibraryType.Movies,
        CollectionTypeOptions.tvshows => LibraryType.Shows,
        CollectionTypeOptions.music => LibraryType.Music,
        CollectionTypeOptions.musicvideos => LibraryType.MusicVideos,
        CollectionTypeOptions.homevideos => LibraryType.HomeVideos,
        _ => null,
    };
}
