using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;

/// <summary>
/// Runs one download job from start to end.
/// </summary>
public interface IDownloadRunner
{
    /// <summary>
    /// Runs a job. Never throws for problems with the download itself; they end the job as failed.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <param name="stoppingToken">Cancelled when Jellyfin shuts down.</param>
    /// <returns>A task that completes when the job has ended.</returns>
    Task RunAsync(DownloadJob job, CancellationToken stoppingToken);
}
