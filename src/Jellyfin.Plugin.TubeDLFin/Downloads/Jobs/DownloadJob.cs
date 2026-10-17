using System;
using System.Collections.Generic;
using System.Threading;
using Jellyfin.Plugin.TubeDLFin.Downloads.Output;
using Jellyfin.Plugin.TubeDLFin.Processes;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;

/// <summary>
/// One download: its plan, its live state and its log. Thread-safe: yt-dlp's two output streams
/// update it while the API reads snapshots. Only snapshots (<see cref="DownloadJobInfo"/>)
/// leave the queue.
/// </summary>
public sealed class DownloadJob : IDisposable
{
    /// <summary>
    /// How many log lines are kept for the job's "Details".
    /// </summary>
    public const int MaxLogLines = 200;

    private readonly Lock _lock = new();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly List<string> _files = [];
    private readonly Queue<string> _log = new();

    private DownloadState _state = DownloadState.Queued;
    private string? _title;
    private string? _stage;
    private ProgressEvent? _progress;
    private double? _fileFraction;
    private int? _itemIndex;
    private int? _itemCount;
    private string? _itemTitle;
    private int _failedItems;
    private int _skippedItems;
    private string? _lastError;
    private string? _error;
    private DateTimeOffset? _startedAt;
    private DateTimeOffset? _finishedAt;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadJob"/> class.
    /// </summary>
    /// <param name="id">The job's id.</param>
    /// <param name="plan">What to download.</param>
    /// <param name="source">Who added it, see <see cref="DownloadSources"/>.</param>
    /// <param name="createdAt">When it was added.</param>
    public DownloadJob(Guid id, DownloadPlan plan, string source, DateTimeOffset createdAt)
    {
        Id = id;
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        Source = source ?? throw new ArgumentNullException(nameof(source));
        CreatedAt = createdAt;
        _title = plan.Title;
    }

    /// <summary>
    /// Gets the job's id.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets what to download.
    /// </summary>
    public DownloadPlan Plan { get; }

    /// <summary>
    /// Gets who added the job.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets when the job was added.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets a token cancelled when the admin cancels the job.
    /// </summary>
    public CancellationToken CancellationToken => _cancellation.Token;

    /// <summary>
    /// Gets a value indicating whether the job has ended.
    /// </summary>
    public bool IsFinished
    {
        get
        {
            lock (_lock)
            {
                return IsFinal(_state);
            }
        }
    }

    /// <summary>
    /// Moves a queued job to running.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <returns><c>false</c> if the job was cancelled while it waited.</returns>
    public bool TryStart(DateTimeOffset now)
    {
        lock (_lock)
        {
            if (_state != DownloadState.Queued)
            {
                return false;
            }

            _state = DownloadState.Downloading;
            _startedAt = now;
            return true;
        }
    }

    /// <summary>
    /// Cancels the job: a queued job ends right away, a running one when yt-dlp has been stopped.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <returns><c>false</c> if the job had already ended.</returns>
    public bool RequestCancel(DateTimeOffset now)
    {
        lock (_lock)
        {
            if (IsFinal(_state))
            {
                return false;
            }

            if (_state == DownloadState.Queued)
            {
                Finish(DownloadState.Cancelled, null, now);
            }
        }

        _cancellation.Cancel();
        return true;
    }

    /// <summary>
    /// Records something yt-dlp reported.
    /// </summary>
    /// <param name="ytDlpEvent">The event.</param>
    public void Apply(YtDlpEvent ytDlpEvent)
    {
        ArgumentNullException.ThrowIfNull(ytDlpEvent);

        lock (_lock)
        {
            if (IsFinal(_state))
            {
                return;
            }

            switch (ytDlpEvent)
            {
                case ItemStartedEvent started:
                    _itemIndex = started.Index;
                    _itemCount = started.Count;
                    _itemTitle = started.Title;
                    _title ??= started.Title;
                    _fileFraction = 0;
                    _progress = null;
                    _stage = null;
                    _state = DownloadState.Downloading;
                    break;
                case ProgressEvent progress:
                    _progress = progress;
                    _fileFraction = progress.Fraction ?? _fileFraction;
                    _stage = null;
                    _state = DownloadState.Downloading;
                    break;
                case PostProcessEvent { Status: "started" } postProcess:
                    _stage = postProcess.Processor;
                    _fileFraction = 1;
                    _state = DownloadState.Processing;
                    break;
                case ItemFinishedEvent finished:
                    _files.Add(finished.FilePath);
                    _title ??= finished.Title;
                    break;
                case ItemSkippedEvent:
                    _skippedItems++;
                    break;
                case ErrorEvent error:
                    _failedItems++;
                    _lastError = error.Message;
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Adds a line to the job's log, dropping the oldest beyond <see cref="MaxLogLines"/>.
    /// </summary>
    /// <param name="line">The line.</param>
    public void AppendLog(string line)
    {
        lock (_lock)
        {
            _log.Enqueue(line);
            while (_log.Count > MaxLogLines)
            {
                _log.Dequeue();
            }
        }
    }

    /// <summary>
    /// Gets the last log lines, oldest first.
    /// </summary>
    /// <returns>The lines.</returns>
    public IReadOnlyList<string> CopyLog()
    {
        lock (_lock)
        {
            return [.. _log];
        }
    }

    /// <summary>
    /// Ends the job after yt-dlp exited, deciding the outcome from what it reported.
    /// </summary>
    /// <param name="exit">How yt-dlp ended.</param>
    /// <param name="explainer">Turns errors into messages for the admin.</param>
    /// <param name="now">The current time.</param>
    public void Complete(ProcessExit exit, IDownloadErrorExplainer explainer, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(exit);
        ArgumentNullException.ThrowIfNull(explainer);

        lock (_lock)
        {
            if (IsFinal(_state))
            {
                return;
            }

            var lastError = _lastError is null ? null : explainer.Explain(_lastError);
            if (_files.Count > 0)
            {
                Finish(
                    _failedItems > 0 ? DownloadState.CompletedWithErrors : DownloadState.Completed,
                    _failedItems > 0 ? $"{_failedItems} item(s) failed. Last error: {lastError}" : null,
                    now);
            }
            else if (_failedItems > 0 || !exit.Succeeded)
            {
                Finish(DownloadState.Failed, lastError ?? $"yt-dlp exited with code {exit.ExitCode} without saying why.", now);
            }
            else if (_skippedItems > 0)
            {
                Finish(DownloadState.AlreadyDownloaded, null, now);
            }
            else
            {
                Finish(DownloadState.Failed, "yt-dlp finished without downloading anything.", now);
            }
        }
    }

    /// <summary>
    /// Ends the job with an error that stopped it before or outside yt-dlp.
    /// </summary>
    /// <param name="message">What went wrong, for the admin.</param>
    /// <param name="now">The current time.</param>
    public void Fail(string message, DateTimeOffset now)
    {
        lock (_lock)
        {
            if (!IsFinal(_state))
            {
                Finish(DownloadState.Failed, message, now);
            }
        }
    }

    /// <summary>
    /// Ends a running job that was cancelled.
    /// </summary>
    /// <param name="now">The current time.</param>
    public void MarkCancelled(DateTimeOffset now)
    {
        lock (_lock)
        {
            if (!IsFinal(_state))
            {
                Finish(DownloadState.Cancelled, null, now);
            }
        }
    }

    /// <summary>
    /// Takes a consistent snapshot of the job.
    /// </summary>
    /// <returns>The snapshot.</returns>
    public DownloadJobInfo Snapshot()
    {
        lock (_lock)
        {
            var running = _state is DownloadState.Downloading or DownloadState.Processing;
            return new DownloadJobInfo
            {
                Id = Id,
                Url = Plan.Url.Value,
                Title = _title,
                Source = Source,
                LibraryName = Plan.Library.Name,
                Destination = Plan.Destination,
                State = _state,
                Stage = running ? _stage : null,
                Percent = Percent(),
                DownloadedBytes = running ? _progress?.DownloadedBytes : null,
                TotalBytes = running ? _progress?.TotalBytes ?? _progress?.EstimatedTotalBytes : null,
                Speed = running ? _progress?.Speed : null,
                Eta = running ? _progress?.Eta : null,
                ItemIndex = _itemIndex,
                ItemCount = _itemCount,
                ItemTitle = _itemTitle,
                Files = [.. _files],
                FailedItems = _failedItems,
                SkippedItems = _skippedItems,
                Error = _error,
                CreatedAt = CreatedAt,
                StartedAt = _startedAt,
                FinishedAt = _finishedAt,
                IsFinished = IsFinal(_state),
            };
        }
    }

    /// <inheritdoc />
    public void Dispose() => _cancellation.Dispose();

    private static bool IsFinal(DownloadState state) =>
        state is not (DownloadState.Queued or DownloadState.Downloading or DownloadState.Processing);

    // Overall progress across the playlist: finished items plus the current item's share.
    private double? Percent()
    {
        if (_state is DownloadState.Completed or DownloadState.CompletedWithErrors or DownloadState.AlreadyDownloaded)
        {
            return 100;
        }

        if (_state is not (DownloadState.Downloading or DownloadState.Processing) || _fileFraction is not { } fraction)
        {
            return null;
        }

        var count = Math.Max(_itemCount ?? 1, 1);
        var done = Math.Clamp((_itemIndex ?? 1) - 1, 0, count - 1);
        return Math.Round((done + fraction) / count * 100, 1);
    }

    private void Finish(DownloadState state, string? error, DateTimeOffset now)
    {
        _state = state;
        _error = error;
        _finishedAt = now;
        _stage = null;
    }
}
