using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Libraries;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// Checks what the admin asks to subscribe to or change.
/// </summary>
public interface ISubscriptionValidator
{
    /// <summary>
    /// Checks a new subscription.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="libraries">The libraries that can be downloaded into.</param>
    /// <returns>The settings, or what is wrong.</returns>
    SubscriptionValidation ValidateNew(SubscriptionRequest request, IReadOnlyList<LibraryInfo> libraries);

    /// <summary>
    /// Checks a change to a subscription.
    /// </summary>
    /// <param name="current">The subscription as it is.</param>
    /// <param name="update">The change.</param>
    /// <param name="libraries">The libraries that can be downloaded into.</param>
    /// <returns>The settings after the change, or what is wrong.</returns>
    SubscriptionValidation ValidateUpdate(Subscription current, SubscriptionUpdate update, IReadOnlyList<LibraryInfo> libraries);
}
