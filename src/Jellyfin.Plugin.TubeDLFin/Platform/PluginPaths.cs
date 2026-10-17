using System;
using System.IO;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.TubeDLFin.Platform;

/// <summary>
/// Resolves the plugin's folders from the server's application paths.
/// </summary>
/// <remarks>
/// The data lives under <see cref="IApplicationPaths.DataPath"/> rather than
/// <c>BasePlugin.DataFolderPath</c>: that one points inside the server's <c>plugins/</c>
/// folder, where Jellyfin treats every sub-folder as a plugin of its own.
/// </remarks>
public sealed class PluginPaths : IPluginPaths
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginPaths"/> class.
    /// </summary>
    /// <param name="applicationPaths">The server's application paths.</param>
    public PluginPaths(IApplicationPaths applicationPaths)
    {
        ArgumentNullException.ThrowIfNull(applicationPaths);

        DataRoot = Path.Combine(applicationPaths.DataPath, PluginIdentity.DataFolderName);
    }

    /// <inheritdoc />
    public string DataRoot { get; }

    /// <inheritdoc />
    public string BinDirectory => Path.Combine(DataRoot, "bin");

    /// <inheritdoc />
    public string CacheDirectory => Path.Combine(DataRoot, "cache");

    /// <inheritdoc />
    public string TempDirectory => Path.Combine(DataRoot, "tmp");

    /// <inheritdoc />
    public string DownloadsTempDirectory => Path.Combine(TempDirectory, "downloads");

    /// <inheritdoc />
    public string ArchiveDirectory => Path.Combine(DataRoot, "archive");
}
