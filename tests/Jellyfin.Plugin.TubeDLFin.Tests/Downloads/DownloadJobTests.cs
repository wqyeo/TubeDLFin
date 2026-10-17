using System;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;
using Jellyfin.Plugin.TubeDLFin.Downloads.Output;
using Jellyfin.Plugin.TubeDLFin.Processes;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Jellyfin.Plugin.TubeDLFin.YtDlp;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Downloads;

public sealed class DownloadJobTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 10, 0, 0, TimeSpan.Zero);
    private static readonly ProcessExit Success = new(0, TimedOut: false);
    private static readonly ProcessExit Failure = new(1, TimedOut: false);

    private readonly YtDlpErrorExplainer _explainer = new();
    private readonly DownloadJob _job = new(Guid.NewGuid(), DownloadPlans.Create() with { Title = "Detected title" }, DownloadSources.Manual, Now);

    public void Dispose() => _job.Dispose();

    [Fact]
    public void NewJob_IsQueued()
    {
        var info = _job.Snapshot();

        Assert.Equal((DownloadState.Queued, "Detected title", "Movies", false), (info.State, info.Title, info.LibraryName, info.IsFinished));
        Assert.Null(info.Percent);
    }

    [Fact]
    public void SingleVideo_ProgressThenCompleted()
    {
        Assert.True(_job.TryStart(Now));
        _job.Apply(new ItemStartedEvent("a", "A", null, null));
        _job.Apply(new ProgressEvent("downloading", 50, 200, null, 1000, 3, null, null));

        var running = _job.Snapshot();
        Assert.Equal((DownloadState.Downloading, 25.0, 50L, 200L, 1000.0), (running.State, running.Percent!.Value, running.DownloadedBytes!.Value, running.TotalBytes!.Value, running.Speed!.Value));

        _job.Apply(new PostProcessEvent("Merger", "started"));
        Assert.Equal((DownloadState.Processing, "Merger", 100.0), (_job.Snapshot().State, _job.Snapshot().Stage, _job.Snapshot().Percent!.Value));

        _job.Apply(new ItemFinishedEvent(new DownloadedItem("/media/movies/x/A [a].mp4") { Id = "a", Title = "A" }));
        _job.Complete(Success, _explainer, Now.AddSeconds(5));

        var done = _job.Snapshot();
        Assert.Equal((DownloadState.Completed, 100.0, true), (done.State, done.Percent!.Value, done.IsFinished));
        Assert.Equal(["/media/movies/x/A [a].mp4"], done.Files);
        Assert.Null(done.Speed);
        Assert.Equal(Now.AddSeconds(5), done.FinishedAt);
    }

    [Fact]
    public void Playlist_OverallPercentCountsFinishedItems()
    {
        _job.TryStart(Now);
        _job.Apply(new ItemStartedEvent("b", "B", 2, 4));
        _job.Apply(new ProgressEvent("downloading", 50, 100, null, null, null, null, null));

        var info = _job.Snapshot();
        Assert.Equal((37.5, 2, 4, "B"), (info.Percent!.Value, info.ItemIndex!.Value, info.ItemCount!.Value, info.ItemTitle));
    }

    [Fact]
    public void TitleFromYtDlp_WhenNoneDetected()
    {
        using var job = new DownloadJob(Guid.NewGuid(), DownloadPlans.Create(), DownloadSources.Manual, Now);
        job.TryStart(Now);
        job.Apply(new ItemStartedEvent("a", "From yt-dlp", null, null));

        Assert.Equal("From yt-dlp", job.Snapshot().Title);
    }

    [Fact]
    public void SomeItemsFailed_CompletedWithErrors()
    {
        _job.TryStart(Now);
        _job.Apply(new ItemFinishedEvent(new DownloadedItem("/media/movies/A.mp4") { Id = "a", Title = "A" }));
        _job.Apply(new ErrorEvent("[youtube] b: Video unavailable"));
        _job.Complete(Failure, _explainer, Now);

        var info = _job.Snapshot();
        Assert.Equal((DownloadState.CompletedWithErrors, 1), (info.State, info.FailedItems));
        Assert.Equal("1 item(s) failed. Last error: [youtube] b: Video unavailable", info.Error);
    }

    [Fact]
    public void NothingDownloaded_FailsWithExplainedError()
    {
        _job.TryStart(Now);
        _job.Apply(new ErrorEvent("unable to write data: [Errno 30] Read-only file system"));
        _job.Complete(Failure, _explainer, Now);

        var info = _job.Snapshot();
        Assert.Equal(DownloadState.Failed, info.State);
        Assert.StartsWith("The library folder is read-only for Jellyfin.", info.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void ExitCodeWithoutError_Fails()
    {
        _job.TryStart(Now);
        _job.Complete(new ProcessExit(2, TimedOut: false), _explainer, Now);

        Assert.Equal("yt-dlp exited with code 2 without saying why.", _job.Snapshot().Error);
    }

    [Fact]
    public void AllSkipped_AlreadyDownloaded()
    {
        _job.TryStart(Now);
        _job.Apply(new ItemSkippedEvent("a: A has already been recorded in the archive"));
        _job.Complete(Success, _explainer, Now);

        Assert.Equal((DownloadState.AlreadyDownloaded, 1), (_job.Snapshot().State, _job.Snapshot().SkippedItems));
    }

    [Fact]
    public void NothingAtAll_Fails()
    {
        _job.TryStart(Now);
        _job.Complete(Success, _explainer, Now);

        Assert.Equal((DownloadState.Failed, "yt-dlp finished without downloading anything."), (_job.Snapshot().State, _job.Snapshot().Error));
    }

    [Fact]
    public void CancelWhileQueued_EndsAtOnceAndCannotStart()
    {
        Assert.True(_job.RequestCancel(Now));

        Assert.Equal(DownloadState.Cancelled, _job.Snapshot().State);
        Assert.True(_job.CancellationToken.IsCancellationRequested);
        Assert.False(_job.TryStart(Now));
    }

    [Fact]
    public void CancelWhileRunning_SignalsThenEnds()
    {
        _job.TryStart(Now);

        Assert.True(_job.RequestCancel(Now));
        Assert.Equal(DownloadState.Downloading, _job.Snapshot().State);
        Assert.True(_job.CancellationToken.IsCancellationRequested);

        _job.MarkCancelled(Now);
        Assert.Equal(DownloadState.Cancelled, _job.Snapshot().State);
        Assert.False(_job.RequestCancel(Now));
    }

    [Fact]
    public void FinishedJob_IgnoresLaterEvents()
    {
        _job.TryStart(Now);
        _job.Fail("Boom", Now);
        _job.Apply(new ErrorEvent("late"));
        _job.Complete(Success, _explainer, Now);

        Assert.Equal((DownloadState.Failed, "Boom", 0), (_job.Snapshot().State, _job.Snapshot().Error, _job.Snapshot().FailedItems));
    }

    [Fact]
    public void Log_KeepsLastLines()
    {
        for (var i = 0; i < DownloadJob.MaxLogLines + 5; i++)
        {
            _job.AppendLog($"line {i}");
        }

        var log = _job.CopyLog();
        Assert.Equal(DownloadJob.MaxLogLines, log.Count);
        Assert.Equal("line 5", log[0]);
    }
}
