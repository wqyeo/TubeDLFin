using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Configuration;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;

/// <summary>
/// Background service that starts queued jobs, oldest first, while fewer than the configured
/// number are running. At startup it deletes partial files left by the previous run.
/// </summary>
public sealed partial class DownloadDispatcher : BackgroundService
{
    private const int MaxConcurrentLimit = 5;

    // The limit is re-read at least this often, so a changed setting applies without a restart.
    private static readonly TimeSpan SlotPollInterval = TimeSpan.FromSeconds(5);

    private readonly IDownloadJobSource _source;
    private readonly IDownloadRunner _runner;
    private readonly IPluginConfigurationProvider _configuration;
    private readonly IPluginPaths _paths;
    private readonly ILogger<DownloadDispatcher> _logger;
    private readonly SemaphoreSlim _slotFreed = new(0);
    private int _running;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadDispatcher"/> class.
    /// </summary>
    /// <param name="source">The queued jobs.</param>
    /// <param name="runner">Runs one job.</param>
    /// <param name="configuration">The settings (how many downloads at once).</param>
    /// <param name="paths">The plugin's folders.</param>
    /// <param name="logger">The logger.</param>
    public DownloadDispatcher(
        IDownloadJobSource source,
        IDownloadRunner runner,
        IPluginConfigurationProvider configuration,
        IPluginPaths paths,
        ILogger<DownloadDispatcher> logger)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets how many jobs are running right now.
    /// </summary>
    public int Running => Volatile.Read(ref _running);

    /// <inheritdoc />
    public override void Dispose()
    {
        _slotFreed.Dispose();
        base.Dispose();
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DeleteLeftovers();
        LogStarted();

        while (!stoppingToken.IsCancellationRequested)
        {
            DownloadJob job;
            try
            {
                job = await _source.NextAsync(stoppingToken).ConfigureAwait(false);
                await WaitForSlotAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (job.IsFinished)
            {
                continue; // Cancelled while it waited.
            }

            Interlocked.Increment(ref _running);
            _ = RunAsync(job, stoppingToken);
        }
    }

    private async Task RunAsync(DownloadJob job, CancellationToken stoppingToken)
    {
        try
        {
            // Leave the dispatch loop right away; the runner's own awaits do the rest.
            await Task.Yield();
            await _runner.RunAsync(job, stoppingToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Decrement(ref _running);
            _slotFreed.Release();
        }
    }

    private async Task WaitForSlotAsync(CancellationToken stoppingToken)
    {
        while (Running >= Limit())
        {
            await _slotFreed.WaitAsync(SlotPollInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private int Limit() => Math.Clamp(_configuration.Current.MaxConcurrentDownloads, 1, MaxConcurrentLimit);

    // Jobs don't survive a restart, so whatever is in their temp folders is orphaned.
    private void DeleteLeftovers()
    {
        try
        {
            if (Directory.Exists(_paths.DownloadsTempDirectory))
            {
                Directory.Delete(_paths.DownloadsTempDirectory, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogLeftoversNotDeleted(ex, _paths.DownloadsTempDirectory);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Download queue started")]
    private partial void LogStarted();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not delete leftover partial downloads in {Path}")]
    private partial void LogLeftoversNotDeleted(Exception exception, string path);
}
