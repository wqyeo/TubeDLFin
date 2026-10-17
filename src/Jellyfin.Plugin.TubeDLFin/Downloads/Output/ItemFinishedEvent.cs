namespace Jellyfin.Plugin.TubeDLFin.Downloads.Output;

/// <summary>
/// An item's final file has been moved into place.
/// </summary>
/// <param name="Item">The file and its metadata.</param>
public sealed record ItemFinishedEvent(DownloadedItem Item) : YtDlpEvent;
