using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.TubeDLFin.Api;

/// <summary>
/// The body of <c>POST /TubeDLFin/Probe</c>.
/// </summary>
public sealed class ProbeRequest
{
    /// <summary>
    /// Gets or sets the link to detect.
    /// </summary>
    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Checked by MediaUrl, which explains what is wrong; a Uri would fail model binding with a generic error.")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a link to a video inside a playlist should be
    /// detected as just the video.
    /// </summary>
    public bool NoPlaylist { get; set; }
}
