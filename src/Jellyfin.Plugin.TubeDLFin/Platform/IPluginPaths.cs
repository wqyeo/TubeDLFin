namespace Jellyfin.Plugin.TubeDLFin.Platform;

/// <summary>
/// Where the plugin keeps its own files on disk. Folders are not created up front; whoever
/// writes into one creates it.
/// </summary>
public interface IPluginPaths
{
    /// <summary>
    /// Gets the root folder for the plugin's data. It is inside the server's data folder, so it
    /// survives plugin updates and, under Docker, container updates.
    /// </summary>
    string DataRoot { get; }

    /// <summary>
    /// Gets the folder holding the tools TubeDLFin manages (yt-dlp, Deno).
    /// </summary>
    string BinDirectory { get; }

    /// <summary>
    /// Gets the folder for yt-dlp's cache.
    /// </summary>
    string CacheDirectory { get; }

    /// <summary>
    /// Gets the folder for work in progress (tool downloads, partial media downloads).
    /// Kept outside the libraries so Jellyfin never scans half-written files.
    /// </summary>
    string TempDirectory { get; }
}
