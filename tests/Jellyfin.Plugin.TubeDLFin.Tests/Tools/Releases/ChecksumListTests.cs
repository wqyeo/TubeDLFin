using Jellyfin.Plugin.TubeDLFin.Tools.Releases;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Tools.Releases;

public class ChecksumListTests
{
    private const string HashA = "1fa6733c37ea6fb51c99ad8fe785e7b7e5f3246c9b980230329d4fb72ed8d4d6";
    private const string HashB = "58162f9bfdc27458ea47bfcb311cf47028f17d8154a8bf7d689861d46399230a";

    [Fact]
    public void Parse_YtDlpStyleList_FindsEachFile()
    {
        var list = ChecksumList.Parse($"{HashA}  yt-dlp\n{HashB}  yt-dlp_linux\n");

        Assert.Equal(HashA, list.Find("yt-dlp"));
        Assert.Equal(HashB, list.Find("yt-dlp_linux"));
    }

    [Fact]
    public void Parse_BinaryModeMarker_IsIgnored()
    {
        Assert.Equal(HashA, ChecksumList.Parse($"{HashA} *deno.zip\r\n").Find("deno.zip"));
    }

    [Fact]
    public void Parse_JunkLines_AreSkipped()
    {
        var list = ChecksumList.Parse($"# comment\nnot-a-hash  file\n\n{HashA}  ok\n");

        Assert.Null(list.Find("file"));
        Assert.Equal(HashA, list.Find("ok"));
    }

    [Fact]
    public void Find_UnlistedFile_IsNull()
    {
        Assert.Null(ChecksumList.Parse($"{HashA}  yt-dlp").Find("yt-dlp_linux"));
    }
}
