using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Configuration;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Downloads;

public sealed class DownloadDispatcherTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly DownloadManager _manager = new(TimeProvider.System);
    private readonly IPluginConfigurationProvider _configuration = Substitute.For<IPluginConfigurationProvider>();
    private readonly IPluginPaths _paths = Substitute.For<IPluginPaths>();
    private readonly BlockingRunner _runner = new();
    private readonly PluginConfiguration _settings = new() { MaxConcurrentDownloads = 1 };
    private readonly DownloadDispatcher _dispatcher;

    public DownloadDispatcherTests()
    {
        _configuration.Current.Returns(_ => _settings);
        _paths.DownloadsTempDirectory.Returns(_temp.Combine("tmp", "downloads"));
        _dispatcher = new DownloadDispatcher(_manager, _runner, _configuration, _paths, NullLogger<DownloadDispatcher>.Instance);
    }

    public void Dispose()
    {
        _dispatcher.Dispose();
        _manager.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public async Task Start_DeletesLeftoverPartialFiles()
    {
        _temp.WriteFile(Path.Combine("tmp", "downloads", "old-job", "video.part"), "partial");

        await _dispatcher.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntil(() => !Directory.Exists(_temp.Combine("tmp", "downloads")));

        await _dispatcher.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RunsOneAtATime_ByDefault()
    {
        await _dispatcher.StartAsync(TestContext.Current.CancellationToken);
        var first = _manager.Enqueue(DownloadPlans.Create(), DownloadSources.Manual);
        var second = _manager.Enqueue(DownloadPlans.Create(), DownloadSources.Manual);

        await WaitUntil(() => _runner.Started.Count == 1);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.Equal([first.Id], _runner.Started);

        _runner.Finish(first.Id);
        await WaitUntil(() => _runner.Started.Count == 2);
        Assert.Equal([first.Id, second.Id], _runner.Started);

        _runner.Finish(second.Id);
        await _dispatcher.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RunsSeveral_WhenConfigured()
    {
        _settings.MaxConcurrentDownloads = 2;
        await _dispatcher.StartAsync(TestContext.Current.CancellationToken);
        for (var i = 0; i < 3; i++)
        {
            _manager.Enqueue(DownloadPlans.Create(), DownloadSources.Manual);
        }

        await WaitUntil(() => _runner.Started.Count == 2);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.Equal(2, _runner.Started.Count);
        Assert.Equal(2, _dispatcher.Running);

        _runner.FinishAll();
        await WaitUntil(() => _runner.Started.Count == 3);
        _runner.FinishAll();
        await _dispatcher.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SkipsJobsCancelledWhileQueued()
    {
        await _dispatcher.StartAsync(TestContext.Current.CancellationToken);
        var running = _manager.Enqueue(DownloadPlans.Create(), DownloadSources.Manual);
        var cancelled = _manager.Enqueue(DownloadPlans.Create(), DownloadSources.Manual);
        var next = _manager.Enqueue(DownloadPlans.Create(), DownloadSources.Manual);
        await WaitUntil(() => _runner.Started.Count == 1);

        _manager.Cancel(cancelled.Id);
        _runner.Finish(running.Id);
        await WaitUntil(() => _runner.Started.Count == 2);

        Assert.Equal([running.Id, next.Id], _runner.Started);
        _runner.FinishAll();
        await _dispatcher.StopAsync(TestContext.Current.CancellationToken);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 200 && !condition(); i++)
        {
            await Task.Delay(25, TestContext.Current.CancellationToken);
        }

        Assert.True(condition(), "Timed out waiting.");
    }

    // A runner whose jobs run until the test finishes them.
    private sealed class BlockingRunner : IDownloadRunner
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource> _running = new();

        public ConcurrentQueue<Guid> Started { get; } = new();

        public Task RunAsync(DownloadJob job, CancellationToken stoppingToken)
        {
            job.TryStart(DateTimeOffset.UtcNow);
            var completion = _running.GetOrAdd(job.Id, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
            Started.Enqueue(job.Id);
            return completion.Task;
        }

        public void Finish(Guid id)
        {
            if (_running.TryRemove(id, out var completion))
            {
                completion.SetResult();
            }
        }

        public void FinishAll()
        {
            foreach (var id in _running.Keys)
            {
                Finish(id);
            }
        }
    }
}
