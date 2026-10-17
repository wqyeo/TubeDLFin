namespace Jellyfin.Plugin.TubeDLFin.Platform;

/// <summary>
/// Where the plugin keeps its own files on disk.
/// </summary>
public interface IPluginPaths
{
    /// <summary>
    /// Gets the root folder for the plugin's data: downloaded tools, cache, temporary files
    /// and saved state. It is inside the server's data folder, so it survives plugin updates
    /// and, under Docker, container updates.
    /// </summary>
    string DataRoot { get; }
}
