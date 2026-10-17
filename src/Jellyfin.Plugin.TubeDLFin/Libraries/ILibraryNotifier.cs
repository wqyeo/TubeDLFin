namespace Jellyfin.Plugin.TubeDLFin.Libraries;

/// <summary>
/// Tells Jellyfin about new files so they appear without a library scan.
/// </summary>
public interface ILibraryNotifier
{
    /// <summary>
    /// Reports a file that was added to a library.
    /// </summary>
    /// <param name="library">The library the file was added to.</param>
    /// <param name="path">The file's full path.</param>
    void FileAdded(LibraryInfo library, string path);
}
