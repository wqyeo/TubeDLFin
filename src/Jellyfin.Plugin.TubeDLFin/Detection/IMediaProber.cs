using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.YtDlp;

namespace Jellyfin.Plugin.TubeDLFin.Detection;

/// <summary>
/// Finds out what a link points to without downloading anything.
/// </summary>
public interface IMediaProber
{
    /// <summary>
    /// How many playlist or channel items the Detect step lists. Listing a long channel takes about
    /// a second per 10 items, so the first detection stays quick; downloads aren't limited by this.
    /// </summary>
    const int DetectEntryLimit = 100;

    /// <summary>
    /// Detects a link.
    /// </summary>
    /// <param name="url">The link.</param>
    /// <param name="noPlaylist">For a link to a video inside a playlist: detect only the video.</param>
    /// <param name="entryLimit">How many playlist or channel items to list, from the first.</param>
    /// <param name="cancellationToken">Cancels the detection and stops yt-dlp.</param>
    /// <returns>What was found.</returns>
    /// <exception cref="ProbeFailedException">yt-dlp refused the link, timed out, or printed something unreadable.</exception>
    /// <exception cref="Tools.ToolNotReadyException">yt-dlp is not installed or not working.</exception>
    Task<ProbeResult> ProbeAsync(MediaUrl url, bool noPlaylist, int entryLimit, CancellationToken cancellationToken);
}
