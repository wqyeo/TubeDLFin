namespace Jellyfin.Plugin.TubeDLFin.Detection;

/// <summary>
/// One format yt-dlp can download for a video: a video stream (with or without audio) or an
/// audio-only stream.
/// </summary>
public sealed record MediaFormat
{
    /// <summary>
    /// Gets yt-dlp's format id, as used with <c>-f</c>.
    /// </summary>
    public required string FormatId { get; init; }

    /// <summary>
    /// Gets the file extension, e.g. <c>mp4</c> or <c>webm</c>.
    /// </summary>
    public required string Extension { get; init; }

    /// <summary>
    /// Gets the site's own label, e.g. <c>1080p</c> or <c>medium, DRC</c>.
    /// </summary>
    public string? Note { get; init; }

    /// <summary>
    /// Gets the width in pixels.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Gets the height in pixels.
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// Gets the frame rate.
    /// </summary>
    public double? Fps { get; init; }

    /// <summary>
    /// Gets the video codec, e.g. <c>avc1.640028</c>, if known.
    /// </summary>
    public string? VideoCodec { get; init; }

    /// <summary>
    /// Gets the audio codec, e.g. <c>opus</c>, if known.
    /// </summary>
    public string? AudioCodec { get; init; }

    /// <summary>
    /// Gets the total bitrate in kbit/s.
    /// </summary>
    public double? Bitrate { get; init; }

    /// <summary>
    /// Gets the audio bitrate in kbit/s.
    /// </summary>
    public double? AudioBitrate { get; init; }

    /// <summary>
    /// Gets the size in bytes, exact or estimated (see <see cref="IsFileSizeApproximate"/>).
    /// </summary>
    public long? FileSize { get; init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="FileSize"/> is an estimate.
    /// </summary>
    public bool IsFileSizeApproximate { get; init; }

    /// <summary>
    /// Gets the dynamic range, e.g. <c>SDR</c> or <c>HDR10</c>.
    /// </summary>
    public string? DynamicRange { get; init; }

    /// <summary>
    /// Gets the audio language code.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Gets how it's downloaded, e.g. <c>https</c> or <c>m3u8_native</c>.
    /// </summary>
    public string? Protocol { get; init; }

    /// <summary>
    /// Gets a value indicating whether the format contains video.
    /// </summary>
    public bool HasVideo { get; init; }

    /// <summary>
    /// Gets a value indicating whether the format contains audio.
    /// </summary>
    public bool HasAudio { get; init; }
}
