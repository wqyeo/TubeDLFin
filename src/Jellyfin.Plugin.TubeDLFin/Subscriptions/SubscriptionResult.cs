using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// What a change to a subscription led to.
/// </summary>
public sealed record SubscriptionResult
{
    /// <summary>
    /// Gets the result when the subscription doesn't exist.
    /// </summary>
    public static SubscriptionResult NotFound { get; } = new() { IsNotFound = true };

    /// <summary>
    /// Gets the subscription after the change, if it was made.
    /// </summary>
    public SubscriptionInfo? Subscription { get; init; }

    /// <summary>
    /// Gets why the change was refused.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether there is no subscription with that id.
    /// </summary>
    public bool IsNotFound { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="subscription">The subscription after the change.</param>
    /// <returns>The result.</returns>
    public static SubscriptionResult Done(SubscriptionInfo subscription) => new() { Subscription = subscription };

    /// <summary>
    /// Creates a refused result.
    /// </summary>
    /// <param name="errors">Why.</param>
    /// <returns>The result.</returns>
    public static SubscriptionResult Invalid(IReadOnlyList<string> errors) => new() { Errors = errors };
}
