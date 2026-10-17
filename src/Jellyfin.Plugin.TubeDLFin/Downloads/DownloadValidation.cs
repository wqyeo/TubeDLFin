using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// The outcome of checking a download request: a plan, or the reasons it was refused.
/// </summary>
/// <param name="Plan">The checked plan, if the request is valid.</param>
/// <param name="Errors">What is wrong, in words for the admin. Empty if valid.</param>
public sealed record DownloadValidation(DownloadPlan? Plan, IReadOnlyList<string> Errors)
{
    /// <summary>
    /// Gets a value indicating whether the request is valid.
    /// </summary>
    public bool IsValid => Plan is not null;
}
