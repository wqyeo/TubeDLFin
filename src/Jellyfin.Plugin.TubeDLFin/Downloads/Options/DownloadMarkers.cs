namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// Prefixes of the lines <see cref="ProgressOptions"/> asks yt-dlp to print.
/// </summary>
public static class DownloadMarkers
{
    /// <summary>
    /// Download progress: <c>status|downloaded|total|estimate|speed|eta|fragment|fragments</c>.
    /// </summary>
    public const string Progress = "[tubedlfin:progress]";

    /// <summary>
    /// Post-processing: <c>processor|status</c>.
    /// </summary>
    public const string PostProcess = "[tubedlfin:postprocess]";

    /// <summary>
    /// An item is about to download: JSON with id, title, playlist_index, n_entries.
    /// </summary>
    public const string Start = "[tubedlfin:start]";

    /// <summary>
    /// An item's final file is in place: JSON with its path and metadata (see <see cref="Output.DownloadedItem"/>).
    /// </summary>
    public const string Done = "[tubedlfin:done]";
}
