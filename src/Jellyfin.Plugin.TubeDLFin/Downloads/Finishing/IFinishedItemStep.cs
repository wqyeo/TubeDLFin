using Jellyfin.Plugin.TubeDLFin.Downloads.Output;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Finishing;

/// <summary>
/// Work done on each finished file, in registration order: writing the <c>.nfo</c> file,
/// fixing subtitle names, and finally telling Jellyfin. A failing step is logged in the job
/// and doesn't stop the others.
/// </summary>
public interface IFinishedItemStep
{
    /// <summary>
    /// Processes a finished file.
    /// </summary>
    /// <param name="plan">The download the file belongs to.</param>
    /// <param name="item">The file and its metadata.</param>
    void Run(DownloadPlan plan, DownloadedItem item);
}
