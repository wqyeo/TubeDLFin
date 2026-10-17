using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// Turns a download plan into yt-dlp options (everything except the shared options and the URL,
/// which <see cref="YtDlp.IYtDlpCommandFactory"/> adds).
/// </summary>
public interface IDownloadArgumentsBuilder
{
    /// <summary>
    /// Builds the options.
    /// </summary>
    /// <param name="plan">The download.</param>
    /// <param name="locations">The job's folders.</param>
    /// <returns>The options, one entry per argument.</returns>
    IReadOnlyList<string> Build(DownloadPlan plan, DownloadLocations locations);
}
