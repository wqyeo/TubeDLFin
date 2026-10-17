namespace Jellyfin.Plugin.TubeDLFin.Tools.Status;

/// <summary>
/// The FFmpeg that Jellyfin uses and TubeDLFin passes to yt-dlp.
/// </summary>
/// <param name="EncoderPath">The ffmpeg executable, if Jellyfin found one.</param>
/// <param name="ProbePath">The ffprobe executable, if Jellyfin found one.</param>
/// <param name="Version">The FFmpeg version, if known.</param>
public sealed record FfmpegStatus(string? EncoderPath, string? ProbePath, string? Version)
{
    /// <summary>
    /// Gets a value indicating whether Jellyfin has a usable FFmpeg.
    /// </summary>
    public bool Found => !string.IsNullOrEmpty(EncoderPath);
}
