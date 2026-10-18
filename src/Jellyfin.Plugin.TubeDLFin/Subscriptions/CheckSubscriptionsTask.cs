using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// Scheduled task that checks the subscriptions that are due. It runs often; each subscription's
/// own interval decides whether it is checked.
/// </summary>
public sealed class CheckSubscriptionsTask : IScheduledTask
{
    private readonly ISubscriptionService _subscriptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckSubscriptionsTask"/> class.
    /// </summary>
    /// <param name="subscriptions">The subscriptions.</param>
    public CheckSubscriptionsTask(ISubscriptionService subscriptions)
    {
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
    }

    /// <inheritdoc />
    public string Name => "Check subscriptions";

    /// <inheritdoc />
    public string Key => "TubeDLFinCheckSubscriptions";

    /// <inheritdoc />
    public string Description => "Checks TubeDLFin's subscriptions that are due and queues their new uploads for download.";

    /// <inheritdoc />
    public string Category => PluginIdentity.Name;

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken) =>
        _subscriptions.CheckDueAsync(progress, cancellationToken);

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() =>
    [
        new TaskTriggerInfo { Type = TaskTriggerInfoType.IntervalTrigger, IntervalTicks = TimeSpan.FromMinutes(15).Ticks },
    ];
}
