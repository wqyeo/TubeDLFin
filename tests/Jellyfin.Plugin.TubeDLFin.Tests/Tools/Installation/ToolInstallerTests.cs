using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;
using Jellyfin.Plugin.TubeDLFin.Tools.Installation;
using Jellyfin.Plugin.TubeDLFin.Tools.Status;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Tools.Installation;

public sealed class ToolInstallerTests : IDisposable
{
    private static readonly byte[] Binary = "#!/bin/sh\necho new\n"u8.ToArray();

    private readonly TempDirectory _temp = new();
    private readonly FakeReleaseClient _releases = new();
    private readonly IToolVersionProbe _probe = Substitute.For<IToolVersionProbe>();
    private readonly ToolInstaller _installer;
    private readonly string _destination;

    public ToolInstallerTests()
    {
        var paths = Substitute.For<IPluginPaths>();
        paths.TempDirectory.Returns(_temp.Combine("tmp"));
        _installer = new ToolInstaller(_releases, _probe, paths);
        _destination = _temp.Combine("bin", "yt-dlp");
        _probe.ProbeAsync(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(ToolProbeResult.Ok("2026.08.19"));
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task InstallAsync_RawBinary_VerifiesAndMovesIntoPlace()
    {
        PublishYtDlp(Binary, FakeReleaseClient.Sha256(Binary));

        var version = await Install(new YtDlpToolDefinition(), new ToolAsset("yt-dlp_linux", "SHA2-256SUMS"));

        Assert.Equal("2026.08.19", version);
        Assert.Equal(Binary, await File.ReadAllBytesAsync(_destination, TestContext.Current.CancellationToken));
        Assert.Empty(Directory.GetFileSystemEntries(_temp.Combine("tmp", "tools")));
        if (!OperatingSystem.IsWindows())
        {
            Assert.True(File.GetUnixFileMode(_destination).HasFlag(UnixFileMode.UserExecute));
        }
    }

    [Fact]
    public async Task InstallAsync_ChecksumMismatch_KeepsExistingTool()
    {
        PublishYtDlp(Binary, new string('0', 64));
        WriteExisting("old");

        var error = await Assert.ThrowsAsync<ToolInstallException>(() => Install(new YtDlpToolDefinition(), new ToolAsset("yt-dlp_linux", "SHA2-256SUMS")));

        Assert.Contains("Checksum mismatch", error.Message, StringComparison.Ordinal);
        Assert.Equal("old", await File.ReadAllTextAsync(_destination, TestContext.Current.CancellationToken));
        await _probe.DidNotReceiveWithAnyArgs().ProbeAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task InstallAsync_FileMissingFromChecksumList_Fails()
    {
        _releases
            .WithAsset("yt-dlp/yt-dlp", "2026.08.19", "yt-dlp_linux", Binary)
            .WithText("yt-dlp/yt-dlp", "2026.08.19", "SHA2-256SUMS", $"{FakeReleaseClient.Sha256(Binary)}  yt-dlp_macos\n");

        var error = await Assert.ThrowsAsync<ToolInstallException>(() => Install(new YtDlpToolDefinition(), new ToolAsset("yt-dlp_linux", "SHA2-256SUMS")));

        Assert.Contains("no entry for yt-dlp_linux", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InstallAsync_DoesNotRun_KeepsExistingTool()
    {
        PublishYtDlp(Binary, FakeReleaseClient.Sha256(Binary));
        WriteExisting("old");
        _probe.ProbeAsync(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(ToolProbeResult.Failed("Exec format error"));

        var error = await Assert.ThrowsAsync<ToolInstallException>(() => Install(new YtDlpToolDefinition(), new ToolAsset("yt-dlp_linux", "SHA2-256SUMS")));

        Assert.Contains("Exec format error", error.Message, StringComparison.Ordinal);
        Assert.Equal("old", await File.ReadAllTextAsync(_destination, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InstallAsync_Zip_ExtractsTheExecutable()
    {
        var zip = MakeZip("deno", Binary);
        _releases
            .WithAsset("denoland/deno", "v2.9.7", "deno-x86_64-unknown-linux-gnu.zip", zip)
            .WithText("denoland/deno", "v2.9.7", "deno-x86_64-unknown-linux-gnu.zip.sha256sum", $"{FakeReleaseClient.Sha256(zip)}  deno-x86_64-unknown-linux-gnu.zip\n");
        var destination = _temp.Combine("bin", "deno");

        await _installer.InstallAsync(
            new DenoToolDefinition(),
            new ToolAsset("deno-x86_64-unknown-linux-gnu.zip", "deno-x86_64-unknown-linux-gnu.zip.sha256sum", "deno"),
            "v2.9.7",
            destination,
            TestContext.Current.CancellationToken);

        Assert.Equal(Binary, await File.ReadAllBytesAsync(destination, TestContext.Current.CancellationToken));
    }

    private static byte[] MakeZip(string entryName, byte[] content)
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var entry = archive.CreateEntry(entryName).Open();
            entry.Write(content);
        }

        return memory.ToArray();
    }

    private void PublishYtDlp(byte[] content, string listedHash) =>
        _releases
            .WithAsset("yt-dlp/yt-dlp", "2026.08.19", "yt-dlp_linux", content)
            .WithText("yt-dlp/yt-dlp", "2026.08.19", "SHA2-256SUMS", $"{listedHash}  yt-dlp_linux\n");

    private void WriteExisting(string content) => _temp.WriteFile(Path.Combine("bin", "yt-dlp"), content);

    private Task<string> Install(IToolDefinition definition, ToolAsset asset) =>
        _installer.InstallAsync(definition, asset, "2026.08.19", _destination, TestContext.Current.CancellationToken);
}
