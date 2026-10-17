using System;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TubeDLFin.Libraries;

/// <summary>
/// <see cref="ILibraryNotifier"/> that refreshes the nearest folder Jellyfin already knows,
/// right away. If Jellyfin knows none of the library's folders yet (a library that was empty
/// when it was created and never scanned since), it queues a library scan instead.
/// </summary>
/// <remarks>
/// Not <c>ILibraryMonitor.ReportFileSystemChanged</c>: that waits for the server's "library
/// monitor delay" (60 seconds by default), then does the same nearest-folder lookup, and does
/// nothing at all for a never-scanned library. Refreshing the library itself doesn't help
/// either: it only rescans the library folders Jellyfin already knows.
/// </remarks>
public sealed partial class JellyfinLibraryNotifier : ILibraryNotifier
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<JellyfinLibraryNotifier> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinLibraryNotifier"/> class.
    /// </summary>
    /// <param name="libraryManager">Jellyfin's library manager.</param>
    /// <param name="logger">The logger.</param>
    public JellyfinLibraryNotifier(ILibraryManager libraryManager, ILogger<JellyfinLibraryNotifier> logger)
    {
        _libraryManager = libraryManager ?? throw new ArgumentNullException(nameof(libraryManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void FileAdded(LibraryInfo library, string path)
    {
        ArgumentNullException.ThrowIfNull(library);
        ArgumentException.ThrowIfNullOrEmpty(path);

        var folder = NearestKnownFolder(library, path);
        if (folder is null)
        {
            LogScanning(path, library.Name);
            _libraryManager.QueueLibraryScan();
            return;
        }

        LogRefreshing(path, folder.Path);

        // Queues a high-priority refresh that also looks for new children.
        folder.ChangedExternally();
    }

    private static bool IsInside(string path, string folder, bool orEqual = false)
    {
        var root = Path.TrimEndingDirectorySeparator(folder);
        return (orEqual && string.Equals(Path.TrimEndingDirectorySeparator(path), root, StringComparison.Ordinal))
            || path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    // Walks up from the file's folder, staying inside the library's folders.
    private BaseItem? NearestKnownFolder(LibraryInfo library, string path)
    {
        var root = library.Folders.FirstOrDefault(folder => IsInside(path, folder));
        if (root is null)
        {
            return null;
        }

        for (var directory = Path.GetDirectoryName(path); directory is not null && IsInside(directory, root, orEqual: true); directory = Path.GetDirectoryName(directory))
        {
            if (_libraryManager.FindByPath(directory, isFolder: true) is { } folder)
            {
                return folder;
            }
        }

        return null;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "New file {Path}: refreshing {Folder}")]
    private partial void LogRefreshing(string path, string folder);

    [LoggerMessage(Level = LogLevel.Information, Message = "New file {Path}: Jellyfin doesn't know the folders of library {Library} yet, queueing a library scan")]
    private partial void LogScanning(string path, string library);
}
