using System;
using MediaBrowser.Controller.MediaEncoding;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Status;

/// <summary>
/// <see cref="IFfmpegLocator"/> backed by Jellyfin's <see cref="IMediaEncoder"/> (jellyfin-ffmpeg in Docker).
/// </summary>
public sealed class JellyfinFfmpegLocator : IFfmpegLocator
{
    private readonly IMediaEncoder _mediaEncoder;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinFfmpegLocator"/> class.
    /// </summary>
    /// <param name="mediaEncoder">Jellyfin's media encoder.</param>
    public JellyfinFfmpegLocator(IMediaEncoder mediaEncoder)
    {
        _mediaEncoder = mediaEncoder ?? throw new ArgumentNullException(nameof(mediaEncoder));
    }

    /// <inheritdoc />
    public FfmpegStatus GetStatus() => new(
        NullIfEmpty(_mediaEncoder.EncoderPath),
        NullIfEmpty(_mediaEncoder.ProbePath),
        _mediaEncoder.EncoderVersion?.ToString());

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
