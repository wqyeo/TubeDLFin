using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// A snapshot of a download job, returned by <c>GET /TubeDLFin/Downloads</c>.
/// </summary>
public sealed record DownloadJobInfo
{
    /// <summary>Gets the job's id.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the link being downloaded.</summary>
    public required Uri Url { get; init; }

    /// <summary>Gets the title: the detected one, or the first item's.</summary>
    public string? Title { get; init; }

    /// <summary>Gets who added the job: <c>Manual</c> for the dashboard.</summary>
    public required string Source { get; init; }

    /// <summary>Gets the name of the library downloaded into.</summary>
    public required string LibraryName { get; init; }

    /// <summary>Gets the folder files go into.</summary>
    public required string Destination { get; init; }

    /// <summary>Gets where the job is.</summary>
    public required DownloadState State { get; init; }

    /// <summary>Gets yt-dlp's name of the running post-processing step (e.g. <c>Merger</c>), while processing.</summary>
    public string? Stage { get; init; }

    /// <summary>Gets the overall progress from 0 to 100, if it can be told.</summary>
    public double? Percent { get; init; }

    /// <summary>Gets the bytes downloaded of the current file.</summary>
    public long? DownloadedBytes { get; init; }

    /// <summary>Gets the current file's size, exact or estimated.</summary>
    public long? TotalBytes { get; init; }

    /// <summary>Gets the download speed in bytes per second.</summary>
    public double? Speed { get; init; }

    /// <summary>Gets the seconds left for the current file.</summary>
    public double? Eta { get; init; }

    /// <summary>Gets the 1-based position of the current item in a playlist.</summary>
    public int? ItemIndex { get; init; }

    /// <summary>Gets how many items are being downloaded.</summary>
    public int? ItemCount { get; init; }

    /// <summary>Gets the current item's title.</summary>
    public string? ItemTitle { get; init; }

    /// <summary>Gets the files placed in the library so far.</summary>
    public IReadOnlyList<string> Files { get; init; } = [];

    /// <summary>Gets how many items failed.</summary>
    public int FailedItems { get; init; }

    /// <summary>Gets how many items were skipped because they were downloaded before.</summary>
    public int SkippedItems { get; init; }

    /// <summary>Gets what went wrong, in words for the admin.</summary>
    public string? Error { get; init; }

    /// <summary>Gets when the job was added.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets when yt-dlp started.</summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>Gets when the job ended.</summary>
    public DateTimeOffset? FinishedAt { get; init; }

    /// <summary>Gets a value indicating whether the job has ended (in any way).</summary>
    public bool IsFinished { get; init; }
}
