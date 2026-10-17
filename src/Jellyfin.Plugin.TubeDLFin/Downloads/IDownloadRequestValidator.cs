using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Libraries;

namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// Checks a download request before anything reaches yt-dlp or the disk.
/// </summary>
public interface IDownloadRequestValidator
{
    /// <summary>
    /// Checks a request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="libraries">The libraries that can be downloaded into.</param>
    /// <returns>The plan, or what is wrong.</returns>
    DownloadValidation Validate(DownloadRequest request, IReadOnlyList<LibraryInfo> libraries);
}
