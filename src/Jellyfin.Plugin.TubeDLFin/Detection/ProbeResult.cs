using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Detection;

/// <summary>
/// What TubeDLFin found at a link, returned by <c>POST /TubeDLFin/Probe</c>.
/// </summary>
public sealed record ProbeResult
{
    /// <summary>
    /// Gets what the link points to.
    /// </summary>
    public required ProbeKind Kind { get; init; }

    /// <summary>
    /// Gets the site's id for the video, playlist or channel.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the canonical page of what was detected.
    /// </summary>
    public Uri? Url { get; init; }

    /// <summary>
    /// Gets the name of yt-dlp's extractor, e.g. <c>Youtube</c> or <c>Soundcloud</c>.
    /// </summary>
    public string? Site { get; init; }

    /// <summary>
    /// Gets the uploader or channel name.
    /// </summary>
    public string? Uploader { get; init; }

    /// <summary>
    /// Gets the uploader's channel page.
    /// </summary>
    public Uri? ChannelUrl { get; init; }

    /// <summary>
    /// Gets a thumbnail image URL.
    /// </summary>
    public Uri? Thumbnail { get; init; }

    /// <summary>
    /// Gets the length in seconds.
    /// </summary>
    public double? Duration { get; init; }

    /// <summary>
    /// Gets the upload date.
    /// </summary>
    public DateOnly? UploadDate { get; init; }

    /// <summary>
    /// Gets the artist(s) of a music track.
    /// </summary>
    public string? Artist { get; init; }

    /// <summary>
    /// Gets the album of a music track.
    /// </summary>
    public string? Album { get; init; }

    /// <summary>
    /// Gets the track title of a music track.
    /// </summary>
    public string? Track { get; init; }

    /// <summary>
    /// Gets the release year of a music track.
    /// </summary>
    public int? ReleaseYear { get; init; }

    /// <summary>
    /// Gets the formats containing video, best first.
    /// </summary>
    public IReadOnlyList<MediaFormat> VideoFormats { get; init; } = [];

    /// <summary>
    /// Gets the audio-only formats, best first.
    /// </summary>
    public IReadOnlyList<MediaFormat> AudioFormats { get; init; } = [];

    /// <summary>
    /// Gets the subtitles the uploader provided.
    /// </summary>
    public IReadOnlyList<SubtitleTrack> Subtitles { get; init; } = [];

    /// <summary>
    /// Gets the automatically generated captions, original language first.
    /// </summary>
    public IReadOnlyList<SubtitleTrack> AutomaticCaptions { get; init; } = [];

    /// <summary>
    /// Gets the items of a playlist or channel, up to the listing limit.
    /// </summary>
    public IReadOnlyList<PlaylistEntry> Entries { get; init; } = [];

    /// <summary>
    /// Gets how many items the playlist or channel holds, if known.
    /// </summary>
    public int? ItemCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Entries"/> stops at the listing limit and the
    /// playlist or channel holds more.
    /// </summary>
    public bool IsTruncated { get; init; }

    /// <summary>
    /// Gets a value indicating whether the link points to both a video and a playlist, so the
    /// admin can choose between them.
    /// </summary>
    public bool HasPlaylistChoice { get; init; }

    /// <summary>
    /// Gets yt-dlp's warnings.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
