using System;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Downloads;

public sealed class DownloadArchiveTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly DownloadArchive _archive;

    public DownloadArchiveTests()
    {
        var paths = Substitute.For<IPluginPaths>();
        paths.ArchiveDirectory.Returns(_temp.Combine("archive"));
        _archive = new DownloadArchive(paths);
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void PrepareFile_CreatesFolderAndNamesFileAfterLibrary()
    {
        var path = _archive.PrepareFile("f137a2dd21bbc1b99aa5c0f6bf02a805");

        Assert.Equal(_temp.Combine("archive", "f137a2dd21bbc1b99aa5c0f6bf02a805.txt"), path);
        Assert.True(System.IO.Directory.Exists(_temp.Combine("archive")));
        Assert.EndsWith("x.txt", _archive.PrepareFile("../x"), StringComparison.Ordinal);
    }

    [Fact]
    public void Read_YtDlpLines()
    {
        _temp.WriteFile("archive/lib.txt", "youtube aqz-KE-bpKQ\r\nyoutube eRsGyueVLvQ\n\n");

        Assert.Equal(["youtube aqz-KE-bpKQ", "youtube eRsGyueVLvQ"], _archive.Read("lib"));
        Assert.Empty(_archive.Read("other"));
    }

    [Theory]
    [InlineData("Youtube", "aqz-KE-bpKQ", "youtube aqz-KE-bpKQ")]
    [InlineData("SoundcloudPlaylist", "123", "soundcloudplaylist 123")]
    public void Key_LowerCaseExtractorSpaceId(string extractor, string id, string key)
    {
        Assert.Equal(key, DownloadArchiveKeys.For(extractor, id));
    }
}
