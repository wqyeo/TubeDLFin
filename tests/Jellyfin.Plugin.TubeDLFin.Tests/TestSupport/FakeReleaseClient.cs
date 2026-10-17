using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Tools.Releases;

namespace Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;

/// <summary>
/// In-memory releases: <c>repository → latest tag</c> and <c>(repository, tag, file) → bytes</c>.
/// </summary>
internal sealed class FakeReleaseClient : IReleaseClient
{
    private readonly Dictionary<string, string> _latest = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Repository, string Tag, string File), byte[]> _assets = [];

    public int DownloadCount { get; private set; }

    public static string Sha256(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    public FakeReleaseClient WithLatest(string repository, string tag)
    {
        _latest[repository] = tag;
        return this;
    }

    public FakeReleaseClient WithAsset(string repository, string tag, string fileName, byte[] content)
    {
        _assets[(repository, tag, fileName)] = content;
        return this;
    }

    public FakeReleaseClient WithText(string repository, string tag, string fileName, string content) =>
        WithAsset(repository, tag, fileName, System.Text.Encoding.UTF8.GetBytes(content));

    public Task<string> GetLatestTagAsync(string repository, CancellationToken cancellationToken) =>
        _latest.TryGetValue(repository, out var tag)
            ? Task.FromResult(tag)
            : throw new HttpRequestException($"No latest release for {repository}");

    public async Task DownloadAssetAsync(string repository, string tag, string fileName, string destinationPath, CancellationToken cancellationToken)
    {
        DownloadCount++;
        await File.WriteAllBytesAsync(destinationPath, Get(repository, tag, fileName), cancellationToken);
    }

    public Task<string> GetAssetTextAsync(string repository, string tag, string fileName, CancellationToken cancellationToken) =>
        Task.FromResult(System.Text.Encoding.UTF8.GetString(Get(repository, tag, fileName)));

    private byte[] Get(string repository, string tag, string fileName) =>
        _assets.TryGetValue((repository, tag, fileName), out var content)
            ? content
            : throw new HttpRequestException($"404 {repository}/{tag}/{fileName}");
}
