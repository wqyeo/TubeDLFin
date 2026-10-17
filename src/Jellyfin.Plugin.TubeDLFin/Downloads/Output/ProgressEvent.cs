namespace Jellyfin.Plugin.TubeDLFin.Downloads.Output;

/// <summary>
/// Progress of the file being downloaded (a video often downloads as two files: video and audio).
/// </summary>
/// <param name="Status"><c>downloading</c>, <c>finished</c> or <c>error</c>.</param>
/// <param name="DownloadedBytes">Bytes downloaded so far.</param>
/// <param name="TotalBytes">The file's size, if known.</param>
/// <param name="EstimatedTotalBytes">An estimate of the size, for streams whose size isn't known.</param>
/// <param name="Speed">Bytes per second.</param>
/// <param name="Eta">Seconds left.</param>
/// <param name="FragmentIndex">The fragment being downloaded, for streamed formats.</param>
/// <param name="FragmentCount">The number of fragments, for streamed formats.</param>
public sealed record ProgressEvent(
    string Status,
    long? DownloadedBytes,
    long? TotalBytes,
    long? EstimatedTotalBytes,
    double? Speed,
    double? Eta,
    int? FragmentIndex,
    int? FragmentCount) : YtDlpEvent
{
    /// <summary>
    /// Gets how much of the file is done, from 0 to 1, if it can be told.
    /// </summary>
    public double? Fraction
    {
        get
        {
            if (Status == "finished")
            {
                return 1;
            }

            if (DownloadedBytes is { } done && (TotalBytes ?? EstimatedTotalBytes) is { } total && total > 0)
            {
                return System.Math.Clamp((double)done / total, 0, 1);
            }

            return FragmentIndex is { } index && FragmentCount is { } count && count > 0
                ? System.Math.Clamp((double)index / count, 0, 1)
                : null;
        }
    }
}
