using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.TubeDLFin.Platform;

namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// <see cref="IDownloadArchive"/> keeping the files in the plugin's archive folder.
/// </summary>
public sealed class DownloadArchive : IDownloadArchive
{
    private readonly IPluginPaths _paths;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadArchive"/> class.
    /// </summary>
    /// <param name="paths">The plugin's folders.</param>
    public DownloadArchive(IPluginPaths paths)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    /// <inheritdoc />
    public string PrepareFile(string libraryId)
    {
        Directory.CreateDirectory(_paths.ArchiveDirectory);
        return FilePath(libraryId);
    }

    /// <inheritdoc />
    public IReadOnlySet<string> Read(string libraryId)
    {
        var path = FilePath(libraryId);
        if (!File.Exists(path))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        // yt-dlp may be appending to the file right now.
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd()
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
    }

    // The library id is Jellyfin's GUID, but keep only safe characters anyway: it names a file.
    private string FilePath(string libraryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(libraryId);

        var name = string.Concat(libraryId.Where(c => char.IsAsciiLetterOrDigit(c) || c == '-'));
        return Path.Combine(_paths.ArchiveDirectory, name + ".txt");
    }
}
