namespace Jellyfin.Plugin.TubeDLFin.Platform;

/// <summary>
/// Reports free disk space.
/// </summary>
public interface IDiskSpaceProvider
{
    /// <summary>
    /// Gets the free space on the disk holding a folder.
    /// </summary>
    /// <param name="path">The folder; if it doesn't exist yet, its nearest existing parent counts.</param>
    /// <returns>The bytes available to Jellyfin, or <c>null</c> if it can't be told.</returns>
    long? GetFreeBytes(string path);
}
