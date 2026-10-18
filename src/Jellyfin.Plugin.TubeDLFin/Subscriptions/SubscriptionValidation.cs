using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// The result of checking a subscription request or update.
/// </summary>
/// <param name="Settings">The checked settings, or <c>null</c> if there are errors.</param>
/// <param name="Errors">What is wrong, as sentences for the admin.</param>
public sealed record SubscriptionValidation(SubscriptionSettings? Settings, IReadOnlyList<string> Errors)
{
    /// <summary>
    /// Gets a value indicating whether the request can be carried out.
    /// </summary>
    public bool IsValid => Settings is not null;
}
