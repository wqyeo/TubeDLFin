using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;

/// <summary>
/// Hands queued jobs to the dispatcher, oldest first.
/// </summary>
public interface IDownloadJobSource
{
    /// <summary>
    /// Waits for the next queued job.
    /// </summary>
    /// <param name="cancellationToken">Stops waiting.</param>
    /// <returns>The job. It may have been cancelled meanwhile; <see cref="DownloadJob.TryStart"/> tells.</returns>
    ValueTask<DownloadJob> NextAsync(CancellationToken cancellationToken);
}
