namespace Jellyfin.Plugin.TubeDLFin.Tools;

/// <summary>
/// The external tools TubeDLFin can manage. FFmpeg is not one of them: Jellyfin provides it.
/// </summary>
public enum ToolKind
{
    /// <summary>
    /// yt-dlp, the downloader.
    /// </summary>
    YtDlp,

    /// <summary>
    /// Deno, the JavaScript runtime yt-dlp needs to solve YouTube's challenges.
    /// </summary>
    Deno,
}
