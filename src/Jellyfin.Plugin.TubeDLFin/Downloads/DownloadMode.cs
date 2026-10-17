namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// What to keep from the download.
/// </summary>
public enum DownloadMode
{
    /// <summary>Video with audio.</summary>
    Video,

    /// <summary>Audio only, extracted from the best audio stream.</summary>
    Audio,
}
