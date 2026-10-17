using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Downloads;

public sealed class DownloadManagerTests : IDisposable
{
    private readonly ManualTimeProvider _time = new(new DateTimeOffset(2026, 10, 17, 10, 0, 0, TimeSpan.Zero));
    private readonly DownloadManager _manager;

    public DownloadManagerTests()
    {
        _manager = new DownloadManager(_time);
    }

    public void Dispose() => _manager.Dispose();

    [Fact]
    public async Task Enqueue_ListsNewestFirstAndHandsOutOldestFirst()
    {
        var first = _manager.Enqueue(DownloadPlans.Create(), DownloadSources.Manual);
        _time.Advance(TimeSpan.FromSeconds(1));
        var second = _manager.Enqueue(DownloadPlans.Create(), DownloadSources.Manual);

        Assert.Equal([second.Id, first.Id], _manager.GetJobs().Select(j => j.Id));
        Assert.Equal(first.Id, (await _manager.NextAsync(TestContext.Current.CancellationToken)).Id);
        Assert.Equal(second.Id, (await _manager.NextAsync(TestContext.Current.CancellationToken)).Id);
    }

    [Fact]
    public async Task Cancel_QueuedJob()
    {
        var job = _manager.Enqueue(DownloadPlans.Create(), DownloadSources.Manual);

        Assert.Equal(DownloadState.Cancelled, _manager.Cancel(job.Id)?.State);
        Assert.True((await _manager.NextAsync(TestContext.Current.CancellationToken)).IsFinished);
        Assert.Null(_manager.Cancel(Guid.NewGuid()));
    }

    [Fact]
    public async Task Remove_OnlyFinishedJobs()
    {
        var job = _manager.Enqueue(DownloadPlans.Create(), DownloadSources.Manual);
        (await _manager.NextAsync(TestContext.Current.CancellationToken)).TryStart(_time.GetUtcNow());

        Assert.Equal(RemoveResult.NotFinished, _manager.Remove(job.Id));
        _manager.Cancel(job.Id);
        Assert.Equal(RemoveResult.NotFinished, _manager.Remove(job.Id)); // still stopping yt-dlp

        Assert.Equal(RemoveResult.NotFound, _manager.Remove(Guid.NewGuid()));
    }

    [Fact]
    public void ClearFinished_KeepsActiveJobs()
    {
        var active = _manager.Enqueue(DownloadPlans.Create(), DownloadSources.Manual);
        var cancelled = _manager.Enqueue(DownloadPlans.Create(), DownloadSources.Manual);
        _manager.Cancel(cancelled.Id);

        Assert.Equal(1, _manager.ClearFinished());
        Assert.Equal([active.Id], _manager.GetJobs().Select(j => j.Id));
        Assert.Null(_manager.GetJob(cancelled.Id));
        Assert.Null(_manager.GetLog(cancelled.Id));
        Assert.Empty(_manager.GetLog(active.Id)!);
    }
}
