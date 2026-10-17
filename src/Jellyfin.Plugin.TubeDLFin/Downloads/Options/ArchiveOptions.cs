using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// Records every downloaded item in the library's download archive and skips items already in it,
/// so nothing is downloaded into the same library twice (also by future subscriptions).
/// </summary>
public sealed class ArchiveOptions : IDownloadOptionSet
{
    /// <inheritdoc />
    public void AddTo(IList<string> arguments, DownloadPlan plan, DownloadLocations locations)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(locations);

        if (locations.ArchiveFile is not null)
        {
            arguments.Add("--download-archive");
            arguments.Add(locations.ArchiveFile);
        }
    }
}
