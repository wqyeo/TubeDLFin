namespace Jellyfin.Plugin.TubeDLFin.Downloads.Output;

/// <summary>
/// yt-dlp reported an error. In a playlist this fails one item, and yt-dlp goes on with the next.
/// </summary>
/// <param name="Message">The error without its prefix.</param>
public sealed record ErrorEvent(string Message) : YtDlpEvent;
