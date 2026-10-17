using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Jellyfin.Plugin.TubeDLFin.Tools.Releases;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Tools.Releases;

public sealed class GitHubReleaseClientTests : IDisposable
{
    private readonly FakeHttpHandler _http = new();
    private readonly GitHubReleaseClient _client;

    public GitHubReleaseClientTests()
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(_http, disposeHandler: false));
        _client = new GitHubReleaseClient(factory);
    }

    public void Dispose() => _http.Dispose();

    [Theory]
    [InlineData("https://github.com/yt-dlp/yt-dlp/releases/tag/2026.08.19", "2026.08.19")]
    [InlineData("https://github.com/denoland/deno/releases/tag/v2.9.7", "v2.9.7")]
    [InlineData("https://github.com/denoland/deno/releases/tag/v2.9.7/", "v2.9.7")]
    public void ParseTagFromReleaseUri_ReleasePage_ReturnsTag(string url, string expected)
    {
        Assert.Equal(expected, GitHubReleaseClient.ParseTagFromReleaseUri(new Uri(url)));
    }

    [Theory]
    [InlineData("https://github.com/yt-dlp/yt-dlp/releases")]
    [InlineData("https://github.com/yt-dlp/yt-dlp/releases/tag/")]
    public void ParseTagFromReleaseUri_NotAReleasePage_IsNull(string url)
    {
        Assert.Null(GitHubReleaseClient.ParseTagFromReleaseUri(new Uri(url)));
    }

    [Fact]
    public async Task GetLatestTagAsync_FollowsLatestRedirect()
    {
        _http.OnRedirect("https://github.com/denoland/deno/releases/latest", "https://github.com/denoland/deno/releases/tag/v2.9.7");

        Assert.Equal("v2.9.7", await _client.GetLatestTagAsync("denoland/deno", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetLatestTagAsync_NoRedirect_Throws()
    {
        _http.OnText("https://github.com/denoland/deno/releases/latest", "<html></html>");

        await Assert.ThrowsAsync<HttpRequestException>(() => _client.GetLatestTagAsync("denoland/deno", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DownloadAssetAsync_WritesFileFromTaggedDownloadUrl()
    {
        using var temp = new TempDirectory();
        _http.OnContent("https://github.com/yt-dlp/yt-dlp/releases/download/2026.08.19/yt-dlp_linux", [1, 2, 3]);

        var destination = temp.Combine("yt-dlp_linux");
        await _client.DownloadAssetAsync("yt-dlp/yt-dlp", "2026.08.19", "yt-dlp_linux", destination, TestContext.Current.CancellationToken);

        Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(destination, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetAssetTextAsync_MissingAsset_Throws()
    {
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _client.GetAssetTextAsync("yt-dlp/yt-dlp", "2026.08.19", "SHA2-256SUMS", TestContext.Current.CancellationToken));
    }
}
