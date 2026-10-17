namespace Jellyfin.Plugin.TubeDLFin.Downloads.Output;

/// <summary>
/// An item (a video, or one item of a playlist) is about to download.
/// </summary>
/// <param name="Id">The site's id for the item.</param>
/// <param name="Title">The item's title.</param>
/// <param name="Index">Its 1-based position among the items being downloaded.</param>
/// <param name="Count">How many items are being downloaded.</param>
public sealed record ItemStartedEvent(string? Id, string? Title, int? Index, int? Count) : YtDlpEvent;
