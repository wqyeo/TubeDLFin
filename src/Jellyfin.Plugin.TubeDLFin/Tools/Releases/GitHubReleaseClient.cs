using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Releases;

/// <summary>
/// <see cref="IReleaseClient"/> for GitHub releases. It uses plain github.com URLs rather than
/// the REST API, so it isn't subject to the API's 60-requests-per-hour anonymous limit.
/// </summary>
public sealed class GitHubReleaseClient : IReleaseClient
{
    private static readonly Uri GitHub = new("https://github.com/");

    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubReleaseClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Jellyfin's HTTP client factory (honours the server's proxy settings).</param>
    public GitHubReleaseClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    /// <summary>
    /// Reads the tag from a release page URL such as <c>https://github.com/o/r/releases/tag/v1.2.3</c>.
    /// </summary>
    /// <param name="uri">The release page URL.</param>
    /// <returns>The tag, or <c>null</c> if the URL is not a release page.</returns>
    public static string? ParseTagFromReleaseUri(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        const string Marker = "/releases/tag/";
        var path = uri.AbsolutePath;
        var index = path.LastIndexOf(Marker, StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }

        var tag = Uri.UnescapeDataString(path[(index + Marker.Length)..].TrimEnd('/'));
        return tag.Length == 0 || tag.Contains('/', StringComparison.Ordinal) ? null : tag;
    }

    /// <inheritdoc />
    public async Task<string> GetLatestTagAsync(string repository, CancellationToken cancellationToken)
    {
        // /releases/latest redirects to /releases/tag/<tag>; only the final URL is needed.
        var client = _httpClientFactory.CreateClient(NamedClient.Default);
        using var response = await client
            .GetAsync(new Uri(GitHub, $"{repository}/releases/latest"), HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var finalUri = response.RequestMessage?.RequestUri
            ?? throw new HttpRequestException($"No final URL for the latest {repository} release.");
        return ParseTagFromReleaseUri(finalUri)
            ?? throw new HttpRequestException($"Could not read the latest {repository} release from {finalUri}.");
    }

    /// <inheritdoc />
    public async Task DownloadAssetAsync(string repository, string tag, string fileName, string destinationPath, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(NamedClient.Default);
        using var response = await client
            .GetAsync(AssetUri(repository, tag, fileName), HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (source.ConfigureAwait(false))
        {
            var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await using (destination.ConfigureAwait(false))
            {
                await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task<string> GetAssetTextAsync(string repository, string tag, string fileName, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(NamedClient.Default);
        return await client.GetStringAsync(AssetUri(repository, tag, fileName), cancellationToken).ConfigureAwait(false);
    }

    private static Uri AssetUri(string repository, string tag, string fileName) =>
        new(GitHub, $"{repository}/releases/download/{Uri.EscapeDataString(tag)}/{Uri.EscapeDataString(fileName)}");
}
