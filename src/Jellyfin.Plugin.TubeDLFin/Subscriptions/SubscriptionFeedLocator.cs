using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.TubeDLFin.YtDlp;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// <see cref="ISubscriptionFeedLocator"/>: the first resolver that knows the link wins. Other
/// links (playlists, other sites) are listed in full, up to <see cref="DefaultListLimit"/> items,
/// since playlists add new items at the end.
/// </summary>
public sealed class SubscriptionFeedLocator : ISubscriptionFeedLocator
{
    /// <summary>
    /// How many items of a link no resolver knows are listed.
    /// </summary>
    public const int DefaultListLimit = 1000;

    private readonly IReadOnlyList<ISubscriptionFeedResolver> _resolvers;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionFeedLocator"/> class.
    /// </summary>
    /// <param name="resolvers">The sites' resolvers.</param>
    public SubscriptionFeedLocator(IEnumerable<ISubscriptionFeedResolver> resolvers)
    {
        ArgumentNullException.ThrowIfNull(resolvers);

        _resolvers = resolvers.ToList();
    }

    /// <inheritdoc />
    public SubscriptionFeed Locate(MediaUrl url)
    {
        ArgumentNullException.ThrowIfNull(url);

        return _resolvers.Select(resolver => resolver.Resolve(url)).FirstOrDefault(feed => feed is not null)
            ?? new SubscriptionFeed(url, DefaultListLimit, NewestFirst: false);
    }
}
