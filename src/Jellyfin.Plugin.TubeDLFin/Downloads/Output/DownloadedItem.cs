using System;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Output;

/// <summary>
/// A finished file and what yt-dlp knows about it: the input for <c>.nfo</c> files and other
/// finishing steps.
/// </summary>
/// <param name="FilePath">The full path of the media file in the library.</param>
public sealed record DownloadedItem(string FilePath)
{
    /// <summary>Gets the site's id for the item.</summary>
    public string? Id { get; init; }

    /// <summary>Gets the title.</summary>
    public string? Title { get; init; }

    /// <summary>Gets the description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the upload date.</summary>
    public DateOnly? UploadDate { get; init; }

    /// <summary>Gets the upload time, if the site gives one.</summary>
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>Gets the uploader or channel name.</summary>
    public string? Uploader { get; init; }

    /// <summary>Gets the length in seconds.</summary>
    public double? Duration { get; init; }

    /// <summary>Gets yt-dlp's extractor name, e.g. <c>Youtube</c>.</summary>
    public string? Extractor { get; init; }

    /// <summary>Gets the artist(s) of a music track.</summary>
    public string? Artist { get; init; }

    /// <summary>Gets the album of a music track.</summary>
    public string? Album { get; init; }

    /// <summary>Gets the track title of a music track.</summary>
    public string? Track { get; init; }

    /// <summary>Gets the release year of a music track.</summary>
    public int? ReleaseYear { get; init; }

    /// <summary>Gets the title of the playlist the item was downloaded from.</summary>
    public string? PlaylistTitle { get; init; }

    /// <summary>Gets the item's position in that playlist.</summary>
    public int? PlaylistIndex { get; init; }
}
