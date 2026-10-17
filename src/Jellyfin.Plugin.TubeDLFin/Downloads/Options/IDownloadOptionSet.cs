using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// One group of yt-dlp options for a download (output paths, progress, format, ...). Add a
/// feature by registering another set; <see cref="DownloadArgumentsBuilder"/> applies them all
/// in registration order.
/// </summary>
public interface IDownloadOptionSet
{
    /// <summary>
    /// Adds this set's options.
    /// </summary>
    /// <param name="arguments">The arguments so far, one entry per argument.</param>
    /// <param name="plan">The download.</param>
    /// <param name="locations">The job's folders.</param>
    void AddTo(IList<string> arguments, DownloadPlan plan, DownloadLocations locations);
}
