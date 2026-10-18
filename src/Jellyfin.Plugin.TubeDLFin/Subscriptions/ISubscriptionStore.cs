using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// Keeps the subscriptions on disk.
/// </summary>
public interface ISubscriptionStore
{
    /// <summary>
    /// Loads the subscriptions.
    /// </summary>
    /// <param name="cancellationToken">Cancels reading.</param>
    /// <returns>The subscriptions, or none if there are none yet or the file is unreadable.</returns>
    Task<IReadOnlyList<Subscription>> LoadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves the subscriptions, replacing what was saved before.
    /// </summary>
    /// <param name="subscriptions">Every subscription.</param>
    /// <param name="cancellationToken">Cancels writing; the previous file is kept.</param>
    /// <returns>A task completing when the file is written.</returns>
    Task SaveAsync(IReadOnlyList<Subscription> subscriptions, CancellationToken cancellationToken);
}
