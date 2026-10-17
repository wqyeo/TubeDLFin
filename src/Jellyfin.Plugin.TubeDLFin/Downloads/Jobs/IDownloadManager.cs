using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;

/// <summary>
/// The download queue as the API and future subscriptions see it. Jobs live in memory, so a
/// server restart clears the list (finished files stay in the library and the archive).
/// </summary>
public interface IDownloadManager
{
    /// <summary>
    /// Adds a download. It starts when a slot is free.
    /// </summary>
    /// <param name="plan">What to download.</param>
    /// <param name="source">Who added it, see <see cref="DownloadSources"/>.</param>
    /// <returns>The new job.</returns>
    DownloadJobInfo Enqueue(DownloadPlan plan, string source);

    /// <summary>
    /// Gets every job, newest first.
    /// </summary>
    /// <returns>Snapshots of the jobs.</returns>
    IReadOnlyList<DownloadJobInfo> GetJobs();

    /// <summary>
    /// Gets one job.
    /// </summary>
    /// <param name="id">The job's id.</param>
    /// <returns>The job, or <c>null</c> if there is none with that id.</returns>
    DownloadJobInfo? GetJob(Guid id);

    /// <summary>
    /// Gets a job's last log lines.
    /// </summary>
    /// <param name="id">The job's id.</param>
    /// <returns>The lines, oldest first, or <c>null</c> if there is no such job.</returns>
    IReadOnlyList<string>? GetLog(Guid id);

    /// <summary>
    /// Cancels a queued or running job; stops yt-dlp if it is running.
    /// </summary>
    /// <param name="id">The job's id.</param>
    /// <returns>The job, or <c>null</c> if there is none with that id.</returns>
    DownloadJobInfo? Cancel(Guid id);

    /// <summary>
    /// Removes a finished job from the list. Its files are kept.
    /// </summary>
    /// <param name="id">The job's id.</param>
    /// <returns>What happened.</returns>
    RemoveResult Remove(Guid id);

    /// <summary>
    /// Removes every finished job from the list. Their files are kept.
    /// </summary>
    /// <returns>How many jobs were removed.</returns>
    int ClearFinished();
}
