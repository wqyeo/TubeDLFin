using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Releases;

/// <summary>
/// Reads published releases of a tool.
/// </summary>
public interface IReleaseClient
{
    /// <summary>
    /// Gets the tag of the newest stable release.
    /// </summary>
    /// <param name="repository">The repository, as <c>owner/name</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The tag, e.g. <c>2026.08.19</c> or <c>v2.9.7</c>.</returns>
    Task<string> GetLatestTagAsync(string repository, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads a release file to disk.
    /// </summary>
    /// <param name="repository">The repository, as <c>owner/name</c>.</param>
    /// <param name="tag">The release tag.</param>
    /// <param name="fileName">The release asset's file name.</param>
    /// <param name="destinationPath">Where to write the file.</param>
    /// <param name="cancellationToken">Cancels the download.</param>
    /// <returns>A task that completes when the file is written.</returns>
    Task DownloadAssetAsync(string repository, string tag, string fileName, string destinationPath, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads a small text release file, such as a checksum list.
    /// </summary>
    /// <param name="repository">The repository, as <c>owner/name</c>.</param>
    /// <param name="tag">The release tag.</param>
    /// <param name="fileName">The release asset's file name.</param>
    /// <param name="cancellationToken">Cancels the download.</param>
    /// <returns>The file's text.</returns>
    Task<string> GetAssetTextAsync(string repository, string tag, string fileName, CancellationToken cancellationToken);
}
