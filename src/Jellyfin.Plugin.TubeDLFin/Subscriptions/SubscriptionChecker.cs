using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Detection;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;
using Jellyfin.Plugin.TubeDLFin.Libraries;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Jellyfin.Plugin.TubeDLFin.YtDlp;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// <see cref="ISubscriptionChecker"/>: lists the feed, picks the new uploads with
/// <see cref="NewUploadSelector"/>, and queues each as an ordinary single-video download with the
/// subscription's profile, checked by the same validator as the Download button.
/// </summary>
public sealed partial class SubscriptionChecker : ISubscriptionChecker
{
    private readonly IFeedReader _feeds;
    private readonly IDownloadArchive _archive;
    private readonly IDownloadManager _downloads;
    private readonly IDownloadRequestValidator _validator;
    private readonly ILibraryCatalog _libraries;
    private readonly TimeProvider _time;
    private readonly ILogger<SubscriptionChecker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionChecker"/> class.
    /// </summary>
    /// <param name="feeds">Lists feeds.</param>
    /// <param name="archive">The libraries' download archives.</param>
    /// <param name="downloads">The download queue.</param>
    /// <param name="validator">Checks each download.</param>
    /// <param name="libraries">The libraries.</param>
    /// <param name="time">The clock.</param>
    /// <param name="logger">The logger.</param>
    public SubscriptionChecker(
        IFeedReader feeds,
        IDownloadArchive archive,
        IDownloadManager downloads,
        IDownloadRequestValidator validator,
        ILibraryCatalog libraries,
        TimeProvider time,
        ILogger<SubscriptionChecker> logger)
    {
        _feeds = feeds ?? throw new ArgumentNullException(nameof(feeds));
        _archive = archive ?? throw new ArgumentNullException(nameof(archive));
        _downloads = downloads ?? throw new ArgumentNullException(nameof(downloads));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _libraries = libraries ?? throw new ArgumentNullException(nameof(libraries));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<CheckOutcome> CheckAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        var now = _time.GetUtcNow();
        if (!MediaUrl.TryCreate(subscription.Url.AbsoluteUri, out var url, out var urlError))
        {
            return new CheckOutcome(now, urlError, []);
        }

        FeedListing listing;
        IReadOnlySet<string> archive;
        try
        {
            listing = await _feeds.ReadAsync(new SubscriptionFeed(url, subscription.ListLimit, subscription.NewestFirst), cancellationToken).ConfigureAwait(false);
            archive = _archive.Read(subscription.Profile.LibraryId);
        }
        catch (ToolNotReadyException ex)
        {
            return new CheckOutcome(now, ex.Message, []) { RetryOnNextRun = true };
        }
        catch (ProbeFailedException ex)
        {
            LogCheckFailed(subscription.Name, ex.Message);
            return new CheckOutcome(now, ex.Message, []);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new CheckOutcome(now, $"Could not read the download archive: {ex.Message}", []);
        }

        var source = DownloadSources.Subscription(subscription.Id);
        var queuedUrls = _downloads.GetJobs()
            .Where(job => !job.IsFinished && job.Source == source)
            .Select(job => job.Url.AbsoluteUri)
            .ToHashSet(StringComparer.Ordinal);
        var picks = NewUploadSelector.Select(listing.Entries, subscription, archive, queuedUrls);
        if (picks.Count == 0)
        {
            return new CheckOutcome(now, null, []);
        }

        var libraries = _libraries.GetLibraries();
        var queued = new List<string>();
        foreach (var entry in picks)
        {
            var request = new DownloadRequest { Url = entry.Url.AbsoluteUri, NoPlaylist = true, Title = entry.Title, Profile = subscription.Profile };
            var validation = _validator.Validate(request, libraries);
            if (!validation.IsValid)
            {
                // The same for every video (a deleted library, say): nothing else can be queued.
                return new CheckOutcome(now, string.Join(" ", validation.Errors), queued);
            }

            _downloads.Enqueue(validation.Plan!, source);
            queued.Add(entry.Key);
        }

        LogQueued(queued.Count, subscription.Name);
        return new CheckOutcome(now, null, queued);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Subscription {Name}: queued {Count} new uploads")]
    private partial void LogQueued(int count, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Subscription {Name} could not be checked: {Error}")]
    private partial void LogCheckFailed(string name, string error);
}
