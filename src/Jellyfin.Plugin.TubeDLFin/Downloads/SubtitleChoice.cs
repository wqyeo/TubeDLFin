namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// Which subtitles to download. They are saved as <c>.srt</c> files next to the video, where
/// Jellyfin finds them.
/// </summary>
public sealed record SubtitleChoice
{
    /// <summary>
    /// Gets the language codes, comma-separated, in yt-dlp's syntax: <c>en,de</c>, a pattern such
    /// as <c>en.*</c>, or <c>all</c>. Blank downloads no subtitles.
    /// </summary>
    public string? Languages { get; init; }

    /// <summary>
    /// Gets a value indicating whether automatically generated captions are used when there are
    /// no uploaded subtitles in a language.
    /// </summary>
    public bool IncludeAutomatic { get; init; }

    /// <summary>
    /// Gets a value indicating whether the subtitles are also embedded in the video file.
    /// </summary>
    public bool Embed { get; init; }
}
