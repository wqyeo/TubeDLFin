using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Downloads.Finishing;
using Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;
using Jellyfin.Plugin.TubeDLFin.Downloads.Options;
using Jellyfin.Plugin.TubeDLFin.Downloads.Output;
using Jellyfin.Plugin.TubeDLFin.Libraries;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Processes;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Jellyfin.Plugin.TubeDLFin.YtDlp;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Downloads;

public sealed class DownloadRunnerTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 10, 0, 0, TimeSpan.Zero);

    private readonly TempDirectory _temp = new();
    private readonly IYtDlpCommandFactory _commands = Substitute.For<IYtDlpCommandFactory>();
    private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();
    private readonly ILibraryNotifier _notifier = Substitute.For<ILibraryNotifier>();
    private readonly IPluginPaths _paths = Substitute.For<IPluginPaths>();
    private readonly IFinishedItemStep _failingStep = Substitute.For<IFinishedItemStep>();
    private readonly DownloadRunner _runner;
    private readonly DownloadJob _job;
    private string? _tempSeenDuringRun;
    private List<string> _arguments = [];

    public DownloadRunnerTests()
    {
        _paths.DownloadsTempDirectory.Returns(_temp.Combine("tmp", "downloads"));
        _paths.ArchiveDirectory.Returns(_temp.Combine("archive"));
        _commands.CreateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<MediaUrl>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _arguments = call.Arg<IEnumerable<string>>()?.ToList() ?? [];
                return new ProcessRequest("yt-dlp", _arguments);
            });

        _runner = new DownloadRunner(
            new DownloadArgumentsBuilder([new OutputOptions(Naming.Catalog()), new ArchiveOptions()]),
            _commands,
            _processRunner,
            new YtDlpOutputParser(),
            [new LibraryRefreshStep(_notifier), _failingStep],
            new YtDlpErrorExplainer(),
            _paths,
            new ManualTimeProvider(Now),
            NullLogger<DownloadRunner>.Instance);

        _job = new DownloadJob(Guid.NewGuid(), DownloadPlans.Create() with { Destination = _temp.Combine("library", "YouTube") }, DownloadSources.Manual, Now);
    }

    public void Dispose()
    {
        _job.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public async Task RunAsync_Success_CompletesAndNotifiesJellyfin()
    {
        var file = Path.Combine(_job.Plan.Destination, "jawed", "Me at the zoo [jNQXAC9IVRw].mp4");
        YtDlpPrints(
            0,
            """[tubedlfin:start]{"id": "jNQXAC9IVRw", "title": "Me at the zoo"}""",
            "[tubedlfin:progress]downloading|1024|223779|NA|405453.3|0|NA|NA",
            "[tubedlfin:postprocess]Merger|started",
            $$"""[tubedlfin:done]{"id": "jNQXAC9IVRw", "title": "Me at the zoo", "filepath": "{{file}}"}""");

        await _runner.RunAsync(_job, TestContext.Current.CancellationToken);

        var info = _job.Snapshot();
        Assert.Equal(DownloadState.Completed, info.State);
        Assert.Equal([file], info.Files);
        _notifier.Received(1).FileAdded(DownloadPlans.Movies, file);
        Assert.True(Directory.Exists(_job.Plan.Destination));
    }

    [Fact]
    public async Task RunAsync_FailingStep_IsLoggedAndOthersRun()
    {
        var file = Path.Combine(_job.Plan.Destination, "a.mp4");
        _failingStep.When(s => s.Run(Arg.Any<DownloadPlan>(), Arg.Any<DownloadedItem>())).Do(_ => throw new IOException("disk on fire"));
        YtDlpPrints(0, $$"""[tubedlfin:done]{"id": "a", "filepath": "{{file}}"}""");

        await _runner.RunAsync(_job, TestContext.Current.CancellationToken);

        Assert.Equal(DownloadState.Completed, _job.Snapshot().State);
        _notifier.Received(1).FileAdded(DownloadPlans.Movies, file);
        Assert.Contains(_job.CopyLog(), line => line.Contains("disk on fire", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_UsesOwnTempFolderAndDeletesIt()
    {
        YtDlpPrints(0, "[download] VZ5022VaMmA: Keynote has already been recorded in the archive");

        await _runner.RunAsync(_job, TestContext.Current.CancellationToken);

        var expectedTemp = Path.Combine(_temp.Combine("tmp", "downloads"), _job.Id.ToString("N"));
        Assert.Equal(expectedTemp, _tempSeenDuringRun);
        Assert.Contains("temp:" + expectedTemp, _arguments);
        Assert.False(Directory.Exists(expectedTemp));
        Assert.Equal(DownloadState.AlreadyDownloaded, _job.Snapshot().State);
    }

    [Fact]
    public async Task RunAsync_UsesLibraryArchive()
    {
        YtDlpPrints(0);

        await _runner.RunAsync(_job, TestContext.Current.CancellationToken);

        Assert.Equal(_temp.Combine("archive", DownloadPlans.Movies.Id + ".txt"), _arguments[_arguments.IndexOf("--download-archive") + 1]);
    }

    [Fact]
    public async Task RunAsync_Redownload_SkipsArchive()
    {
        using var job = new DownloadJob(Guid.NewGuid(), _job.Plan with { Redownload = true }, DownloadSources.Manual, Now);
        YtDlpPrints(0);

        await _runner.RunAsync(job, TestContext.Current.CancellationToken);

        Assert.DoesNotContain("--download-archive", _arguments);
    }

    [Fact]
    public async Task RunAsync_LogsLinesButNotProgress()
    {
        YtDlpPrints(0, "[youtube] jNQXAC9IVRw: Downloading webpage", "[tubedlfin:progress]downloading|1|2|NA|NA|NA|NA|NA");

        await _runner.RunAsync(_job, TestContext.Current.CancellationToken);

        Assert.Equal(["[youtube] jNQXAC9IVRw: Downloading webpage"], _job.CopyLog());
    }

    [Fact]
    public async Task RunAsync_Cancelled_StopsAndMarksCancelled()
    {
        _processRunner.StreamAsync(Arg.Any<ProcessRequest>(), Arg.Any<Action<ProcessOutputLine>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _job.RequestCancel(Now);
                return Task.FromException<ProcessExit>(new OperationCanceledException(call.Arg<CancellationToken>()));
            });

        await _runner.RunAsync(_job, TestContext.Current.CancellationToken);

        Assert.Equal(DownloadState.Cancelled, _job.Snapshot().State);
    }

    [Fact]
    public async Task RunAsync_ToolNotReady_Fails()
    {
        _commands.CreateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<MediaUrl>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ToolNotReadyException("yt-dlp is not installed yet."));

        await _runner.RunAsync(_job, TestContext.Current.CancellationToken);

        Assert.Equal((DownloadState.Failed, "yt-dlp is not installed yet."), (_job.Snapshot().State, _job.Snapshot().Error));
    }

    [Fact]
    public async Task RunAsync_DestinationNotWritable_FailsWithoutRunningYtDlp()
    {
        // A file where the destination folder should be: creating the folder fails.
        _temp.WriteFile("blocked", "not a folder");
        using var job = new DownloadJob(Guid.NewGuid(), _job.Plan with { Destination = _temp.Combine("blocked", "YouTube") }, DownloadSources.Manual, Now);

        await _runner.RunAsync(job, TestContext.Current.CancellationToken);

        Assert.Equal(DownloadState.Failed, job.Snapshot().State);
        await _processRunner.DidNotReceiveWithAnyArgs().StreamAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RunAsync_CancelledWhileQueued_DoesNothing()
    {
        _job.RequestCancel(Now);

        await _runner.RunAsync(_job, TestContext.Current.CancellationToken);

        await _commands.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    private void YtDlpPrints(int exitCode, params string[] lines) =>
        _processRunner.StreamAsync(Arg.Any<ProcessRequest>(), Arg.Any<Action<ProcessOutputLine>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _tempSeenDuringRun = Directory.GetDirectories(_temp.Combine("tmp", "downloads")).SingleOrDefault();
                var onLine = call.Arg<Action<ProcessOutputLine>>();
                foreach (var line in lines)
                {
                    onLine(new ProcessOutputLine(line, IsError: false));
                }

                return Task.FromResult(new ProcessExit(exitCode, TimedOut: false));
            });
}
