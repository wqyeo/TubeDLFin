using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// A feed's videos, in the order the site lists them.
/// </summary>
/// <param name="Name">A name for the subscription: the channel's, or the playlist's title.</param>
/// <param name="Entries">The videos.</param>
public sealed record FeedListing(string Name, IReadOnlyList<FeedEntry> Entries);
