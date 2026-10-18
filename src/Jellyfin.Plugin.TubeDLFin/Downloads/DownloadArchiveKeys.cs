using System;

namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// Builds download archive entries the way yt-dlp does: the extractor's key in lower case, a
/// space and the item's id, e.g. <c>youtube aqz-KE-bpKQ</c>.
/// </summary>
public static class DownloadArchiveKeys
{
    /// <summary>
    /// Builds an archive key.
    /// </summary>
    /// <param name="extractor">yt-dlp's extractor key, e.g. <c>Youtube</c>.</param>
    /// <param name="id">The item's id on the site.</param>
    /// <returns>The key.</returns>
    public static string For(string extractor, string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extractor);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

#pragma warning disable CA1308 // yt-dlp's format is lower case; this isn't a comparison.
        return extractor.ToLowerInvariant() + " " + id;
#pragma warning restore CA1308
    }
}
