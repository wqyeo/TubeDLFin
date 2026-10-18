using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;
using Jellyfin.Plugin.TubeDLFin.Libraries;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// <see cref="ISubscriptionService"/> keeping the subscriptions in memory, loaded on first use and
/// saved after every change. Changes are made on a copy that replaces the list only once saved.
/// </summary>
public sealed partial class SubscriptionService : ISubscriptionService, IDisposable
{
    private readonly SemaphoreSlim _state = new(1, 1);
    private readonly SemaphoreSlim _checks = new(1, 1);
    private readonly ISubscriptionStore _store;
    private readonly ISubscriptionValidator _validator;
    private readonly ISubscriptionFeedLocator _feeds;
    private readonly IFeedReader _reader;
    private readonly ISubscriptionChecker _checker;
    private readonly IDownloadArchive _archive;
    private readonly IDownloadManager _downloads;
    private readonly ILibraryCatalog _libraries;
    private readonly TimeProvider _time;
    private readonly ILogger<SubscriptionService> _logger;
    private List<Subscription>? _subscriptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionService"/> class.
    /// </summary>
    /// <param name="store">Keeps the subscriptions on disk.</param>
    /// <param name="validator">Checks requests.</param>
    /// <param name="feeds">Finds the link to check for a channel or playlist.</param>
    /// <param name="reader">Lists feeds.</param>
    /// <param name="checker">Checks a subscription.</param>
    /// <param name="archive">The libraries' download archives.</param>
    /// <param name="downloads">The download queue.</param>
    /// <param name="libraries">The libraries.</param>
    /// <param name="time">The clock.</param>
    /// <param name="logger">The logger.</param>
    public SubscriptionService(
        ISubscriptionStore store,
        ISubscriptionValidator validator,
        ISubscriptionFeedLocator feeds,
        IFeedReader reader,
        ISubscriptionChecker checker,
        IDownloadArchive archive,
        IDownloadManager downloads,
        ILibraryCatalog libraries,
        TimeProvider time,
        ILogger<SubscriptionService> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _feeds = feeds ?? throw new ArgumentNullException(nameof(feeds));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _checker = checker ?? throw new ArgumentNullException(nameof(checker));
        _archive = archive ?? throw new ArgumentNullException(nameof(archive));
        _downloads = downloads ?? throw new ArgumentNullException(nameof(downloads));
        _libraries = libraries ?? throw new ArgumentNullException(nameof(libraries));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SubscriptionInfo>> GetAllAsync(CancellationToken cancellationToken)
    {
        var subscriptions = await SnapshotAsync(cancellationToken).ConfigureAwait(false);
        var info = InfoBuilder();
        return subscriptions.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase).Select(info).ToList();
    }

    /// <inheritdoc />
    public async Task<SubscriptionResult> AddAsync(SubscriptionRequest request, CancellationToken cancellationToken)
    {
        var validation = _validator.ValidateNew(request, _libraries.GetLibraries());
        if (!validation.IsValid)
        {
            return SubscriptionResult.Invalid(validation.Errors);
        }

        var settings = validation.Settings!;
        var feed = _feeds.Locate(settings.Url);
        if (IsDuplicate(await SnapshotAsync(cancellationToken).ConfigureAwait(false), feed, settings))
        {
            return SubscriptionResult.Invalid(["You are already subscribed to this in that library."]);
        }

        var listing = await _reader.ReadAsync(feed, cancellationToken).ConfigureAwait(false);
        var now = _time.GetUtcNow();
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = settings.Name ?? Shorten(listing.Name),
            Url = feed.Url.Value,
            ListLimit = feed.ListLimit,
            NewestFirst = feed.NewestFirst,
            Profile = settings.Profile,
            IntervalHours = settings.IntervalHours,
            CreatedAt = now,
            NextCheckAt = now.AddHours(settings.IntervalHours),
            Baseline = listing.Entries.Select(entry => entry.Key).Distinct(StringComparer.Ordinal).ToList(),
        };

        var added = await ChangeAsync(
            list =>
            {
                // Two clicks on "Subscribe" at once: only the first is saved.
                if (IsDuplicate(list, feed, settings))
                {
                    return null;
                }

                list.Add(subscription);
                return subscription;
            },
            cancellationToken).ConfigureAwait(false);
        if (added is null)
        {
            return SubscriptionResult.Invalid(["You are already subscribed to this in that library."]);
        }

        LogSubscribed(subscription.Name, subscription.Url, subscription.Baseline.Count);
        return SubscriptionResult.Done(InfoBuilder()(subscription));
    }

    /// <inheritdoc />
    public async Task<SubscriptionResult> UpdateAsync(Guid id, SubscriptionUpdate update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);

        var libraries = _libraries.GetLibraries();
        IReadOnlyList<string> errors = [];
        var updated = await ChangeAsync(
            list =>
            {
                var index = list.FindIndex(s => s.Id == id);
                if (index < 0)
                {
                    return null;
                }

                var current = list[index];
                var validation = _validator.ValidateUpdate(current, update, libraries);
                if (!validation.IsValid)
                {
                    errors = validation.Errors;
                    return null;
                }

                var settings = validation.Settings!;
                list[index] = current with
                {
                    Name = settings.Name ?? current.Name,
                    IntervalHours = settings.IntervalHours,
                    Profile = settings.Profile,
                    NextCheckAt = settings.IntervalHours == current.IntervalHours
                        ? current.NextCheckAt
                        : (current.LastCheckedAt ?? current.CreatedAt).AddHours(settings.IntervalHours),
                };
                return list[index];
            },
            cancellationToken).ConfigureAwait(false);

        return Result(updated, errors);
    }

    /// <inheritdoc />
    public async Task<SubscriptionResult> SetPausedAsync(Guid id, bool paused, CancellationToken cancellationToken)
    {
        var updated = await ChangeAsync(
            list =>
            {
                var index = list.FindIndex(s => s.Id == id);
                if (index < 0)
                {
                    return null;
                }

                list[index] = list[index] with { Paused = paused };
                return list[index];
            },
            cancellationToken).ConfigureAwait(false);

        return Result(updated, []);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var removed = await ChangeAsync(
            list =>
            {
                var index = list.FindIndex(s => s.Id == id);
                if (index < 0)
                {
                    return null;
                }

                var subscription = list[index];
                list.RemoveAt(index);
                return subscription;
            },
            cancellationToken).ConfigureAwait(false);

        if (removed is not null)
        {
            LogUnsubscribed(removed.Name);
        }

        return removed is not null;
    }

    /// <inheritdoc />
    public async Task<SubscriptionResult> CheckNowAsync(Guid id, CancellationToken cancellationToken)
    {
        await _checks.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var subscription = (await SnapshotAsync(cancellationToken).ConfigureAwait(false)).Find(s => s.Id == id);
            var checkedSubscription = subscription is null ? null : await CheckAndRecordAsync(subscription, cancellationToken).ConfigureAwait(false);
            return Result(checkedSubscription, []);
        }
        finally
        {
            _checks.Release();
        }
    }

    /// <inheritdoc />
    public async Task CheckDueAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);

        await _checks.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _time.GetUtcNow();
            var due = (await SnapshotAsync(cancellationToken).ConfigureAwait(false)).Where(s => s.IsDue(now)).ToList();
            for (var i = 0; i < due.Count; i++)
            {
                await CheckAndRecordAsync(due[i], cancellationToken).ConfigureAwait(false);
                progress.Report(100.0 * (i + 1) / due.Count);
            }
        }
        finally
        {
            _checks.Release();
        }

        progress.Report(100);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _state.Dispose();
        _checks.Dispose();
    }

    private static bool IsDuplicate(IEnumerable<Subscription> subscriptions, SubscriptionFeed feed, SubscriptionSettings settings) =>
        subscriptions.Any(s => s.Url == feed.Url.Value && string.Equals(s.Profile.LibraryId, settings.Profile.LibraryId, StringComparison.Ordinal));

    private static string Shorten(string name) =>
        name.Length > SubscriptionValidator.MaxNameLength ? name[..SubscriptionValidator.MaxNameLength] : name;

    private SubscriptionResult Result(Subscription? subscription, IReadOnlyList<string> errors) =>
        errors.Count > 0 ? SubscriptionResult.Invalid(errors)
        : subscription is null ? SubscriptionResult.NotFound
        : SubscriptionResult.Done(InfoBuilder()(subscription));

    private async Task<Subscription?> CheckAndRecordAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        CheckOutcome outcome;
        try
        {
            outcome = await _checker.CheckAsync(subscription, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031 // One broken subscription must not stop the others; the error is shown to the admin.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogUnexpected(ex, subscription.Name);
            outcome = new CheckOutcome(_time.GetUtcNow(), $"Unexpected error: {ex.Message}", []);
        }

        // Downloads may have been queued: record them even if the caller gave up meanwhile.
        return await ChangeAsync(
            list =>
            {
                var index = list.FindIndex(s => s.Id == subscription.Id);
                if (index < 0)
                {
                    return null; // Deleted during the check.
                }

                list[index] = list[index].After(outcome);
                return list[index];
            },
            CancellationToken.None).ConfigureAwait(false);
    }

    // Runs a change on a copy of the list, saves the copy if the change returned something, and
    // only then makes it the current list.
    private async Task<Subscription?> ChangeAsync(Func<List<Subscription>, Subscription?> change, CancellationToken cancellationToken)
    {
        await _state.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var copy = new List<Subscription>(await LoadedAsync(cancellationToken).ConfigureAwait(false));
            var result = change(copy);
            if (result is not null)
            {
                await _store.SaveAsync(copy, cancellationToken).ConfigureAwait(false);
                _subscriptions = copy;
            }

            return result;
        }
        finally
        {
            _state.Release();
        }
    }

    private async Task<List<Subscription>> SnapshotAsync(CancellationToken cancellationToken)
    {
        await _state.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return new List<Subscription>(await LoadedAsync(cancellationToken).ConfigureAwait(false));
        }
        finally
        {
            _state.Release();
        }
    }

    // Callers hold _state.
    private async Task<List<Subscription>> LoadedAsync(CancellationToken cancellationToken) =>
        _subscriptions ??= [.. await _store.LoadAsync(cancellationToken).ConfigureAwait(false)];

    // Builds the page's view of subscriptions, reading each library's archive once.
    private Func<Subscription, SubscriptionInfo> InfoBuilder()
    {
        var libraryNames = _libraries.GetLibraries()
            .DistinctBy(library => library.Id, StringComparer.Ordinal)
            .ToDictionary(library => library.Id, library => library.Name, StringComparer.Ordinal);
        var queued = _downloads.GetJobs()
            .Where(job => !job.IsFinished)
            .CountBy(job => job.Source, StringComparer.Ordinal)
            .ToDictionary(StringComparer.Ordinal);
        var archives = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);

        return subscription =>
        {
            var libraryId = subscription.Profile.LibraryId;
            if (!archives.TryGetValue(libraryId, out var archive))
            {
                archive = ReadArchive(libraryId);
                archives[libraryId] = archive;
            }

            return new SubscriptionInfo
            {
                Id = subscription.Id,
                Name = subscription.Name,
                Url = subscription.Url,
                LibraryName = libraryNames.GetValueOrDefault(libraryId),
                IntervalHours = subscription.IntervalHours,
                Paused = subscription.Paused,
                CreatedAt = subscription.CreatedAt,
                LastCheckedAt = subscription.LastCheckedAt,
                NextCheckAt = subscription.NextCheckAt,
                LastError = subscription.LastError,
                Downloaded = subscription.CountDownloaded(archive),
                Queued = queued.GetValueOrDefault(DownloadSources.Subscription(subscription.Id)),
                GivenUp = subscription.CountGivenUp(archive),
                Profile = subscription.Profile,
            };
        };
    }

    private IReadOnlySet<string> ReadArchive(string libraryId)
    {
        try
        {
            return _archive.Read(libraryId);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogArchiveUnreadable(ex, libraryId);
            return new HashSet<string>();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Subscribed to {Name} ({Url}); its {Count} current videos won't be downloaded")]
    private partial void LogSubscribed(string name, Uri url, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted the subscription {Name}")]
    private partial void LogUnsubscribed(string name);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error checking the subscription {Name}")]
    private partial void LogUnexpected(Exception exception, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not read the download archive of library {LibraryId}")]
    private partial void LogArchiveUnreadable(Exception exception, string libraryId);
}
