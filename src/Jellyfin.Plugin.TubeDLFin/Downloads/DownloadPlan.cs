using Jellyfin.Plugin.TubeDLFin.Libraries;
using Jellyfin.Plugin.TubeDLFin.YtDlp;

namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// A download request after every value has been checked: what the queue runs.
/// </summary>
/// <param name="Url">The link.</param>
/// <param name="Profile">Quality, file type and destination.</param>
/// <param name="Library">The library downloaded into.</param>
/// <param name="Destination">The full path of the folder files go into; inside one of the library's folders.</param>
public sealed record DownloadPlan(MediaUrl Url, DownloadProfile Profile, LibraryInfo Library, string Destination)
{
    /// <summary>
    /// Gets a value indicating whether a link to a video inside a playlist downloads only the video.
    /// </summary>
    public bool NoPlaylist { get; init; }

    /// <summary>
    /// Gets a specific format id, or <c>null</c> to choose by the profile.
    /// </summary>
    public string? FormatId { get; init; }

    /// <summary>
    /// Gets which playlist items to download, or <c>null</c> for all.
    /// </summary>
    public string? PlaylistItems { get; init; }

    /// <summary>
    /// Gets a value indicating whether the download archive is ignored.
    /// </summary>
    public bool Redownload { get; init; }

    /// <summary>
    /// Gets the title to show until yt-dlp reports its own.
    /// </summary>
    public string? Title { get; init; }
}
