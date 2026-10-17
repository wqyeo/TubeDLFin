namespace Jellyfin.Plugin.TubeDLFin.Detection;

/// <summary>
/// Why detecting a link failed.
/// </summary>
public enum ProbeFailure
{
    /// <summary>yt-dlp refused the link: unsupported site, private or removed video, blocked, ...</summary>
    Rejected,

    /// <summary>yt-dlp did not finish in time.</summary>
    TimedOut,

    /// <summary>yt-dlp's output could not be read.</summary>
    UnreadableOutput,
}
