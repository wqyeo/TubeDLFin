using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Downloads.Finishing;
using Jellyfin.Plugin.TubeDLFin.Downloads.Options;
using Jellyfin.Plugin.TubeDLFin.Downloads.Output;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Processes;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Jellyfin.Plugin.TubeDLFin.YtDlp;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;

/// <summary>
/// <see cref="IDownloadRunner"/>: prepares the folders, runs yt-dlp, follows its output, tells
/// Jellyfin about each finished file, and cleans up the job's temp folder.
/// </summary>
public sealed partial class DownloadRunner : IDownloadRunner
{
    private readonly IDownloadArgumentsBuilder _arguments;
    private readonly IYtDlpCommandFactory _commands;
    private readonly IProcessRunner _processRunner;
    private readonly IYtDlpOutputParser _parser;
    private readonly IEnumerable<IFinishedItemStep> _finishingSteps;
    private readonly IYtDlpErrorExplainer _explainer;
    private readonly IDownloadArchive _archive;
    private readonly IPluginPaths _paths;
    private readonly TimeProvider _time;
    private readonly ILogger<DownloadRunner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadRunner"/> class.
    /// </summary>
    /// <param name="arguments">Builds the download options.</param>
    /// <param name="commands">Builds the yt-dlp command.</param>
    /// <param name="processRunner">Runs yt-dlp.</param>
    /// <param name="parser">Reads yt-dlp's output.</param>
    /// <param name="finishingSteps">Run on each finished file, in order (the last tells Jellyfin).</param>
    /// <param name="explainer">Turns errors into advice.</param>
    /// <param name="archive">The libraries' download archives.</param>
    /// <param name="paths">The plugin's folders.</param>
    /// <param name="time">The clock.</param>
    /// <param name="logger">The logger.</param>
    public DownloadRunner(
        IDownloadArgumentsBuilder arguments,
        IYtDlpCommandFactory commands,
        IProcessRunner processRunner,
        IYtDlpOutputParser parser,
        IEnumerable<IFinishedItemStep> finishingSteps,
        IYtDlpErrorExplainer explainer,
        IDownloadArchive archive,
        IPluginPaths paths,
        TimeProvider time,
        ILogger<DownloadRunner> logger)
    {
        _arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _finishingSteps = finishingSteps ?? throw new ArgumentNullException(nameof(finishingSteps));
        _explainer = explainer ?? throw new ArgumentNullException(nameof(explainer));
        _archive = archive ?? throw new ArgumentNullException(nameof(archive));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task RunAsync(DownloadJob job, CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (!job.TryStart(_time.GetUtcNow()))
        {
            return; // Cancelled while queued.
        }

        LogStarting(job.Id, job.Plan.Url.Value, job.Plan.Destination);
        var tempDirectory = Path.Combine(_paths.DownloadsTempDirectory, job.Id.ToString("N"));
        try
        {
            Directory.CreateDirectory(job.Plan.Destination);
            Directory.CreateDirectory(tempDirectory);

            var archive = job.Plan.Redownload ? null : _archive.PrepareFile(job.Plan.Library.Id);

            var options = _arguments.Build(job.Plan, new DownloadLocations(tempDirectory, archive));
            var request = await _commands.CreateAsync(options, job.Plan.Url, stoppingToken).ConfigureAwait(false);

            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(job.CancellationToken, stoppingToken);
            var exit = await _processRunner.StreamAsync(request, line => OnLine(job, line), cancellation.Token).ConfigureAwait(false);

            job.Complete(exit, _explainer, _time.GetUtcNow());
        }
        catch (OperationCanceledException) when (job.CancellationToken.IsCancellationRequested)
        {
            job.MarkCancelled(_time.GetUtcNow());
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            job.Fail("Jellyfin was shut down while downloading.", _time.GetUtcNow());
        }
        catch (ToolNotReadyException ex)
        {
            job.Fail(ex.Message, _time.GetUtcNow());
        }
        catch (Win32Exception ex)
        {
            job.Fail($"Could not start yt-dlp: {ex.Message}", _time.GetUtcNow());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            job.Fail(_explainer.Explain(ex.Message), _time.GetUtcNow());
        }
#pragma warning disable CA1031 // Whatever happens, the job must end and the queue go on; the error is shown to the admin.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogUnexpected(ex, job.Id);
            job.Fail($"Unexpected error: {ex.Message}", _time.GetUtcNow());
        }
        finally
        {
            DeleteTemp(tempDirectory);
        }

        var result = job.Snapshot();
        LogFinished(job.Id, result.State, result.Files.Count, result.Error ?? "none");
    }

    private void OnLine(DownloadJob job, ProcessOutputLine line)
    {
        var ytDlpEvent = _parser.Parse(line);

        // Progress lines arrive many times a second; they'd push everything else out of the log.
        if (ytDlpEvent is not ProgressEvent)
        {
            job.AppendLog(line.Text);
        }

        if (ytDlpEvent is null)
        {
            return;
        }

        job.Apply(ytDlpEvent);
        if (ytDlpEvent is ItemFinishedEvent finished)
        {
            Finish(job, finished.Item);
        }
    }

    // A failing step (say, an .nfo that can't be written) is noted in the job's log; the file
    // itself is fine, so the job goes on and the remaining steps still run.
    private void Finish(DownloadJob job, DownloadedItem item)
    {
        foreach (var step in _finishingSteps)
        {
            try
            {
                step.Run(job.Plan, item);
            }
#pragma warning disable CA1031 // Reported in the job's log; must not stop the download.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogStepFailed(ex, step.GetType().Name, item.FilePath);
                job.AppendLog($"TubeDLFin: {step.GetType().Name} failed for {item.FilePath}: {ex.Message}");
            }
        }
    }

    private void DeleteTemp(string tempDirectory)
    {
        try
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogTempNotDeleted(ex, tempDirectory);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Download {JobId}: {Url} into {Destination}")]
    private partial void LogStarting(Guid jobId, Uri url, string destination);

    [LoggerMessage(Level = LogLevel.Information, Message = "Download {JobId} ended: {State}, {FileCount} file(s), error: {Error}")]
    private partial void LogFinished(Guid jobId, DownloadState state, int fileCount, string error);

    [LoggerMessage(Level = LogLevel.Error, Message = "Download {JobId} failed unexpectedly")]
    private partial void LogUnexpected(Exception exception, Guid jobId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Step} failed for {Path}")]
    private partial void LogStepFailed(Exception exception, string step, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not delete the temp folder {Path}")]
    private partial void LogTempNotDeleted(Exception exception, string path);
}
