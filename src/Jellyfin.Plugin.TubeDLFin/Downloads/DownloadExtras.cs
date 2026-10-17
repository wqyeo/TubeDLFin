namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// What is saved besides the media file. The defaults suit video libraries; music libraries
/// embed the thumbnail as cover art instead of writing <c>.nfo</c> files and posters.
/// </summary>
public sealed record DownloadExtras
{
    /// <summary>
    /// Gets a value indicating whether an <c>.nfo</c> file with title, description and date is
    /// written next to each video, so Jellyfin shows them instead of guessing.
    /// </summary>
    public bool WriteNfo { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the thumbnail is saved as the item's poster image.
    /// </summary>
    public bool SaveThumbnail { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the thumbnail is embedded in the file (cover art).
    /// </summary>
    public bool EmbedThumbnail { get; init; }

    /// <summary>
    /// Gets a value indicating whether title, artist, date and description are embedded as tags.
    /// </summary>
    public bool EmbedMetadata { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether chapters are embedded.
    /// </summary>
    public bool EmbedChapters { get; init; } = true;
}
