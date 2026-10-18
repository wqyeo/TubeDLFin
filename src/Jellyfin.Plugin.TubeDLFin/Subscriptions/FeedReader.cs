using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Detection;
using Jellyfin.Plugin.TubeDLFin.Downloads;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// <see cref="IFeedReader"/> using the Detect step's flat listing, which takes about a second for
/// 50 videos because it doesn't open each one.
/// </summary>
public sealed class FeedReader : IFeedReader
{
    private readonly IMediaProber _prober;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedReader"/> class.
    /// </summary>
    /// <param name="prober">Lists links.</param>
    public FeedReader(IMediaProber prober)
    {
        _prober = prober ?? throw new ArgumentNullException(nameof(prober));
    }

    /// <inheritdoc />
    public async Task<FeedListing> ReadAsync(SubscriptionFeed feed, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(feed);

        var result = await _prober.ProbeAsync(feed.Url, noPlaylist: false, feed.ListLimit, cancellationToken).ConfigureAwait(false);
        if (result.Kind == ProbeKind.Video)
        {
            throw new ProbeFailedException(ProbeFailure.Rejected, "This link is a single video. Subscribe to a channel or a playlist.");
        }

        var entries = result.Entries
            .Where(entry => !entry.IsCollection && entry.Id is not null && entry.Extractor is not null && entry.Url is not null)
            .Select(entry => new FeedEntry(DownloadArchiveKeys.For(entry.Extractor!, entry.Id!), entry.Title, entry.Url!))
            .ToList();
        if (entries.Count == 0 && result.Entries.Any(entry => entry.IsCollection))
        {
            throw new ProbeFailedException(ProbeFailure.Rejected, "This link lists playlists or tabs, not videos. Open one of them and subscribe to that.");
        }

        // A channel tab is titled "Channel - Videos"; the channel's own name reads better.
        var name = result.Kind == ProbeKind.Channel ? result.Uploader ?? result.Title : result.Title;
        return new FeedListing(name, entries);
    }
}
