using System;

namespace Jellyfin.Plugin.TubeDLFin.Detection;

/// <summary>
/// One item of a playlist or channel, as listed without opening it.
/// </summary>
public sealed record PlaylistEntry
{
    /// <summary>
    /// Gets the 1-based position, as used with <c>--playlist-items</c>.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Gets the site's id for the item.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Gets yt-dlp's extractor for the item (<c>Youtube</c>), which with <see cref="Id"/> makes its
    /// download archive entry.
    /// </summary>
    public string? Extractor { get; init; }

    /// <summary>
    /// Gets the title, or the id if the site gave none.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the item's page, if it is an http(s) link.
    /// </summary>
    public Uri? Url { get; init; }

    /// <summary>
    /// Gets the length in seconds.
    /// </summary>
    public double? Duration { get; init; }

    /// <summary>
    /// Gets the uploader or channel name.
    /// </summary>
    public string? Uploader { get; init; }

    /// <summary>
    /// Gets a value indicating whether the item is itself a list (a channel tab or a playlist)
    /// that can be detected on its own.
    /// </summary>
    public bool IsCollection { get; init; }

    /// <summary>
    /// Gets how many items a collection holds, if known.
    /// </summary>
    public int? ItemCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="ItemCount"/> is only a lower bound, because
    /// listing stopped at the limit.
    /// </summary>
    public bool IsItemCountMinimum { get; init; }
}
