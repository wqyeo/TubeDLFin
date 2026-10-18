using System;
using System.Globalization;

namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// Who added a download job.
/// </summary>
public static class DownloadSources
{
    /// <summary>
    /// The admin, from the dashboard page.
    /// </summary>
    public const string Manual = "Manual";

    private const string SubscriptionPrefix = "Subscription:";

    /// <summary>
    /// A subscription's check.
    /// </summary>
    /// <param name="subscriptionId">The subscription's id.</param>
    /// <returns><c>Subscription:</c> followed by the id.</returns>
    public static string Subscription(Guid subscriptionId) =>
        SubscriptionPrefix + subscriptionId.ToString("N", CultureInfo.InvariantCulture);
}
