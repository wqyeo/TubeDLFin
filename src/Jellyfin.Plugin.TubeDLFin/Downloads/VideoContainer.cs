namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// The file type for video downloads.
/// </summary>
public enum VideoContainer
{
    /// <summary>Whatever the chosen streams fit in (yt-dlp's choice, often WebM or MKV).</summary>
    Auto,

    /// <summary>MP4: plays on the most devices.</summary>
    Mp4,

    /// <summary>MKV: holds any codec.</summary>
    Mkv,

    /// <summary>WebM: VP9/AV1 with Opus.</summary>
    Webm,
}
