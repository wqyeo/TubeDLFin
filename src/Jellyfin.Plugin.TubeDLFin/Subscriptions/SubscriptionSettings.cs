using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.YtDlp;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// A subscription's settings after every value has been checked.
/// </summary>
/// <param name="Url">The link the admin entered.</param>
/// <param name="Name">The name, or <c>null</c> to take the feed's.</param>
/// <param name="IntervalHours">The hours between checks.</param>
/// <param name="Profile">How new uploads are downloaded.</param>
public sealed record SubscriptionSettings(MediaUrl Url, string? Name, int IntervalHours, DownloadProfile Profile);
