namespace Jellyfin.Plugin.TubeDLFin.Downloads.Output;

/// <summary>
/// An item was skipped because the download archive says it was downloaded before.
/// </summary>
/// <param name="Message">yt-dlp's message, naming the item.</param>
public sealed record ItemSkippedEvent(string Message) : YtDlpEvent;
