using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// The subscriptions, as the API and the scheduled task use them. Only one check runs at a time.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Lists the subscriptions by name.
    /// </summary>
    /// <param name="cancellationToken">Cancels loading.</param>
    /// <returns>The subscriptions.</returns>
    Task<IReadOnlyList<SubscriptionInfo>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Subscribes to a channel or playlist. Its current videos are recorded and never downloaded;
    /// later uploads are.
    /// </summary>
    /// <param name="request">What to subscribe to, and how.</param>
    /// <param name="cancellationToken">Stops listing the feed.</param>
    /// <returns>The new subscription, or why it was refused.</returns>
    /// <exception cref="Detection.ProbeFailedException">The feed couldn't be listed.</exception>
    /// <exception cref="Tools.ToolNotReadyException">yt-dlp is not installed or not working.</exception>
    Task<SubscriptionResult> AddAsync(SubscriptionRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Changes a subscription's name, interval or download options.
    /// </summary>
    /// <param name="id">The subscription's id.</param>
    /// <param name="update">The change.</param>
    /// <param name="cancellationToken">Cancels saving.</param>
    /// <returns>The subscription after the change, or why it was refused.</returns>
    Task<SubscriptionResult> UpdateAsync(Guid id, SubscriptionUpdate update, CancellationToken cancellationToken);

    /// <summary>
    /// Pauses or resumes a subscription's checks.
    /// </summary>
    /// <param name="id">The subscription's id.</param>
    /// <param name="paused">Whether to pause.</param>
    /// <param name="cancellationToken">Cancels saving.</param>
    /// <returns>The subscription after the change.</returns>
    Task<SubscriptionResult> SetPausedAsync(Guid id, bool paused, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a subscription. Downloaded files, the download archive and queued downloads are kept.
    /// </summary>
    /// <param name="id">The subscription's id.</param>
    /// <param name="cancellationToken">Cancels saving.</param>
    /// <returns><c>false</c> if there is no subscription with that id.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Checks a subscription now, paused or not, after any check already running.
    /// </summary>
    /// <param name="id">The subscription's id.</param>
    /// <param name="cancellationToken">Stops the check.</param>
    /// <returns>The subscription after the check; problems are in its last error.</returns>
    Task<SubscriptionResult> CheckNowAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Checks every subscription that is due, one after another.
    /// </summary>
    /// <param name="progress">Receives the percentage done.</param>
    /// <param name="cancellationToken">Stops checking.</param>
    /// <returns>A task completing when all are checked.</returns>
    Task CheckDueAsync(IProgress<double> progress, CancellationToken cancellationToken);
}
