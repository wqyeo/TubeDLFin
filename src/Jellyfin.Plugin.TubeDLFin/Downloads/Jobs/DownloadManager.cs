using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;

/// <summary>
/// In-memory <see cref="IDownloadManager"/> and <see cref="IDownloadJobSource"/>.
/// </summary>
public sealed class DownloadManager : IDownloadManager, IDownloadJobSource, IDisposable
{
    private readonly ConcurrentDictionary<Guid, DownloadJob> _jobs = new();
    private readonly Channel<DownloadJob> _pending = Channel.CreateUnbounded<DownloadJob>(new UnboundedChannelOptions { SingleReader = true });
    private readonly TimeProvider _time;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadManager"/> class.
    /// </summary>
    /// <param name="time">The clock.</param>
    public DownloadManager(TimeProvider time)
    {
        _time = time ?? throw new ArgumentNullException(nameof(time));
    }

    /// <inheritdoc />
    public DownloadJobInfo Enqueue(DownloadPlan plan, string source)
    {
        var job = new DownloadJob(Guid.NewGuid(), plan, source, _time.GetUtcNow());
        _jobs[job.Id] = job;
        if (!_pending.Writer.TryWrite(job))
        {
            throw new InvalidOperationException("The download queue is closed.");
        }

        return job.Snapshot();
    }

    /// <inheritdoc />
    public IReadOnlyList<DownloadJobInfo> GetJobs() =>
        _jobs.Values.Select(job => job.Snapshot()).OrderByDescending(job => job.CreatedAt).ToList();

    /// <inheritdoc />
    public DownloadJobInfo? GetJob(Guid id) => _jobs.TryGetValue(id, out var job) ? job.Snapshot() : null;

    /// <inheritdoc />
    public IReadOnlyList<string>? GetLog(Guid id) => _jobs.TryGetValue(id, out var job) ? job.CopyLog() : null;

    /// <inheritdoc />
    public DownloadJobInfo? Cancel(Guid id)
    {
        if (!_jobs.TryGetValue(id, out var job))
        {
            return null;
        }

        job.RequestCancel(_time.GetUtcNow());
        return job.Snapshot();
    }

    /// <inheritdoc />
    public RemoveResult Remove(Guid id)
    {
        if (!_jobs.TryGetValue(id, out var job))
        {
            return RemoveResult.NotFound;
        }

        if (!job.IsFinished)
        {
            return RemoveResult.NotFinished;
        }

        if (_jobs.TryRemove(id, out var removed))
        {
            removed.Dispose();
        }

        return RemoveResult.Removed;
    }

    /// <inheritdoc />
    public int ClearFinished() =>
        _jobs.Values.Where(job => job.IsFinished).Count(job => Remove(job.Id) == RemoveResult.Removed);

    /// <inheritdoc />
    public ValueTask<DownloadJob> NextAsync(CancellationToken cancellationToken) => _pending.Reader.ReadAsync(cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        _pending.Writer.TryComplete();
        foreach (var job in _jobs.Values)
        {
            job.Dispose();
        }
    }
}
