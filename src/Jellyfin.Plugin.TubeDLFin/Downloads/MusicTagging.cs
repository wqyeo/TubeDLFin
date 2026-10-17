namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// How music is tagged. Jellyfin builds music libraries from the files' tags, and most videos
/// only have a title like "Artist - Title (Official Video)", so TubeDLFin fills in the gaps.
/// </summary>
public sealed record MusicTagging
{
    /// <summary>
    /// Gets a value indicating whether music tagging applies (audio downloads and music videos).
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets a value indicating whether a title like "Artist - Title" is split into artist and
    /// title, when the site gives no artist.
    /// </summary>
    public bool SplitArtistTitle { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether "(Official Video)", "(Lyrics)" and the like are removed
    /// from titles.
    /// </summary>
    public bool CleanTitles { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether a playlist's items are numbered as tracks by their position.
    /// </summary>
    public bool NumberTracks { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether a playlist becomes one album by "Various Artists", named
    /// after the playlist, even if its tracks belong to other albums.
    /// </summary>
    public bool PlaylistAsAlbum { get; init; }

    /// <summary>
    /// Gets a value indicating whether embedded cover art is cropped to a square (video thumbnails
    /// are 16:9 and look letterboxed as album art).
    /// </summary>
    public bool SquareCover { get; init; } = true;
}
