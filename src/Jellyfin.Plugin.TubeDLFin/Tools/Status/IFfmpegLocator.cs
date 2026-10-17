namespace Jellyfin.Plugin.TubeDLFin.Tools.Status;

/// <summary>
/// Finds the FFmpeg that Jellyfin uses.
/// </summary>
public interface IFfmpegLocator
{
    /// <summary>
    /// Gets Jellyfin's FFmpeg paths and version.
    /// </summary>
    /// <returns>The status.</returns>
    FfmpegStatus GetStatus();
}
