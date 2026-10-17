namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// Where a download job is.
/// </summary>
public enum DownloadState
{
    /// <summary>Waiting for a free slot.</summary>
    Queued,

    /// <summary>yt-dlp is downloading.</summary>
    Downloading,

    /// <summary>yt-dlp is merging, converting or moving files.</summary>
    Processing,

    /// <summary>Everything was downloaded.</summary>
    Completed,

    /// <summary>Some playlist items were downloaded, others failed.</summary>
    CompletedWithErrors,

    /// <summary>Nothing new: every item was in the library's download archive.</summary>
    AlreadyDownloaded,

    /// <summary>Nothing was downloaded because of an error.</summary>
    Failed,

    /// <summary>The admin cancelled it.</summary>
    Cancelled,
}
