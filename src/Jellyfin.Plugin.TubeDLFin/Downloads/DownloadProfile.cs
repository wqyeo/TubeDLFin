using Jellyfin.Plugin.TubeDLFin.Downloads.Naming;

namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// The reusable part of a download's options: quality, file type and destination. A one-off
/// download is a profile plus a link; a subscription will be a profile plus a channel.
/// </summary>
public sealed record DownloadProfile
{
    /// <summary>
    /// Gets what to keep: video or audio only.
    /// </summary>
    public DownloadMode Mode { get; init; }

    /// <summary>
    /// Gets the highest video height to download, e.g. 1080, or <c>null</c> for the best available.
    /// </summary>
    public int? MaxHeight { get; init; }

    /// <summary>
    /// Gets the file type for video.
    /// </summary>
    public VideoContainer Container { get; init; }

    /// <summary>
    /// Gets a value indicating whether H.264 video and AAC audio are preferred, because more
    /// devices play them without transcoding. YouTube offers H.264 only up to 1080p.
    /// </summary>
    public bool PreferH264 { get; init; }

    /// <summary>
    /// Gets the file type for audio-only downloads.
    /// </summary>
    public AudioFormat AudioFormat { get; init; }

    /// <summary>
    /// Gets the audio bitrate in kbit/s for lossy re-encoding, or <c>null</c> for the best.
    /// </summary>
    public int? AudioQuality { get; init; }

    /// <summary>
    /// Gets Jellyfin's id of the library to download into.
    /// </summary>
    public string LibraryId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the library folder to download into; one of the library's folders.
    /// </summary>
    public string LibraryFolder { get; init; } = string.Empty;

    /// <summary>
    /// Gets an optional folder inside the library folder, e.g. <c>YouTube/Talks</c>.
    /// </summary>
    public string? Subfolder { get; init; }

    /// <summary>
    /// Gets how files are named inside the destination folder.
    /// </summary>
    public NamingPresetId Naming { get; init; }

    /// <summary>
    /// Gets the yt-dlp output template when <see cref="Naming"/> is <see cref="NamingPresetId.Custom"/>.
    /// </summary>
    public string? CustomTemplate { get; init; }

    /// <summary>
    /// Gets what is saved besides the media file.
    /// </summary>
    public DownloadExtras Extras { get; init; } = new();

    /// <summary>
    /// Gets which subtitles to download.
    /// </summary>
    public SubtitleChoice Subtitles { get; init; } = new();

    /// <summary>
    /// Gets what to do with sponsor segments.
    /// </summary>
    public SponsorBlockChoice SponsorBlock { get; init; } = new();

    /// <summary>
    /// Gets how music is tagged.
    /// </summary>
    public MusicTagging Music { get; init; } = new();
}
