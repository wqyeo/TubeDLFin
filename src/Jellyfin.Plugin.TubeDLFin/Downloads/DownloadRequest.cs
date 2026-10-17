using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// The body of <c>POST /TubeDLFin/Downloads</c>: a link, link-specific choices and a profile.
/// </summary>
public sealed record DownloadRequest
{
    /// <summary>
    /// Gets the link to download.
    /// </summary>
    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Checked by MediaUrl, which explains what is wrong.")]
    public string? Url { get; init; }

    /// <summary>
    /// Gets a value indicating whether a link to a video inside a playlist downloads only the video.
    /// </summary>
    public bool NoPlaylist { get; init; }

    /// <summary>
    /// Gets a specific format id from the Detect step, or <c>null</c> to choose by the profile.
    /// </summary>
    public string? FormatId { get; init; }

    /// <summary>
    /// Gets which playlist items to download, in yt-dlp's syntax (<c>1-10,15,20:</c>), or <c>null</c> for all.
    /// </summary>
    public string? PlaylistItems { get; init; }

    /// <summary>
    /// Gets a value indicating whether items already downloaded into this library are downloaded again.
    /// </summary>
    public bool Redownload { get; init; }

    /// <summary>
    /// Gets the title shown in the queue until yt-dlp reports its own.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the quality, file type and destination.
    /// </summary>
    public DownloadProfile? Profile { get; init; }
}
