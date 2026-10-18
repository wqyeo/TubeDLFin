using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Libraries;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// <see cref="ISubscriptionValidator"/>. The link and download options are checked by the same
/// <see cref="IDownloadRequestValidator"/> as the Download button.
/// </summary>
public sealed class SubscriptionValidator : ISubscriptionValidator
{
    /// <summary>
    /// The longest name accepted.
    /// </summary>
    public const int MaxNameLength = 200;

    private readonly IDownloadRequestValidator _downloads;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionValidator"/> class.
    /// </summary>
    /// <param name="downloads">Checks links and download options.</param>
    public SubscriptionValidator(IDownloadRequestValidator downloads)
    {
        _downloads = downloads ?? throw new ArgumentNullException(nameof(downloads));
    }

    /// <inheritdoc />
    public SubscriptionValidation ValidateNew(SubscriptionRequest request, IReadOnlyList<LibraryInfo> libraries)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<string>();
        var name = Name(request.Name, required: false, errors);
        var interval = Interval(request.IntervalHours ?? Subscription.DefaultIntervalHours, errors);
        var plan = Plan(request.Url, request.Profile, libraries, errors);

        return errors.Count > 0
            ? new SubscriptionValidation(null, errors)
            : new SubscriptionValidation(new SubscriptionSettings(plan!.Url, name, interval, plan.Profile), []);
    }

    /// <inheritdoc />
    public SubscriptionValidation ValidateUpdate(Subscription current, SubscriptionUpdate update, IReadOnlyList<LibraryInfo> libraries)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(update);

        var errors = new List<string>();
        var name = update.Name is null ? current.Name : Name(update.Name, required: true, errors);
        var interval = Interval(update.IntervalHours ?? current.IntervalHours, errors);

        // Only new options are checked: renaming must work even if the library has gone.
        var plan = update.Profile is null ? null : Plan(current.Url.AbsoluteUri, update.Profile, libraries, errors);
        if (errors.Count > 0)
        {
            return new SubscriptionValidation(null, errors);
        }

        return YtDlp.MediaUrl.TryCreate(current.Url.AbsoluteUri, out var url, out var urlError)
            ? new SubscriptionValidation(new SubscriptionSettings(url, name, interval, plan?.Profile ?? current.Profile), [])
            : new SubscriptionValidation(null, [urlError]);
    }

    private static string? Name(string? name, bool required, List<string> errors)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            if (required)
            {
                errors.Add("Enter a name.");
            }

            return null;
        }

        if (trimmed.Length > MaxNameLength || trimmed.Any(char.IsControl))
        {
            errors.Add($"The name must be at most {MaxNameLength} characters, without line breaks.");
        }

        return trimmed;
    }

    private static int Interval(int hours, List<string> errors)
    {
        if (hours is < Subscription.MinIntervalHours or > Subscription.MaxIntervalHours)
        {
            errors.Add($"Check every {Subscription.MinIntervalHours} to {Subscription.MaxIntervalHours} hours.");
        }

        return hours;
    }

    private DownloadPlan? Plan(string? url, DownloadProfile? profile, IReadOnlyList<LibraryInfo> libraries, List<string> errors)
    {
        var validation = _downloads.Validate(new DownloadRequest { Url = url, Profile = profile }, libraries);
        errors.AddRange(validation.Errors);
        return validation.Plan;
    }
}
