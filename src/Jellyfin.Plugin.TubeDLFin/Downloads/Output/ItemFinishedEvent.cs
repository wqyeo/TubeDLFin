namespace Jellyfin.Plugin.TubeDLFin.Downloads.Output;

/// <summary>
/// An item's final file has been moved into place.
/// </summary>
/// <param name="Id">The site's id for the item.</param>
/// <param name="Title">The item's title.</param>
/// <param name="FilePath">The full path of the file.</param>
public sealed record ItemFinishedEvent(string? Id, string? Title, string FilePath) : YtDlpEvent;
