using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// Checks one subscription for new uploads and queues them.
/// </summary>
public interface ISubscriptionChecker
{
    /// <summary>
    /// Checks a subscription. Problems (the site, yt-dlp, a deleted library) are reported in the
    /// outcome rather than thrown.
    /// </summary>
    /// <param name="subscription">The subscription.</param>
    /// <param name="cancellationToken">Stops the check.</param>
    /// <returns>What the check did.</returns>
    Task<CheckOutcome> CheckAsync(Subscription subscription, CancellationToken cancellationToken);
}
