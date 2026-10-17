using Jellyfin.Plugin.TubeDLFin.YtDlp;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.YtDlp;

public class YtDlpMessagesTests
{
    private const string Stderr =
        "WARNING: [youtube:tab] YouTube Music is not directly supported.\r\n"
        + "[youtube] Extracting URL\n"
        + "ERROR: [youtube] abc: Video unavailable\n"
        + "WARNING: [youtube:tab] YouTube Music is not directly supported.\n"
        + "ERROR: [vimeo] 76979871: Got HTTP Error 403; please report this issue on  https://github.com/yt-dlp/yt-dlp/issues?q= , filling out the template\n";

    [Fact]
    public void LastError_ReturnsLastErrorWithoutBoilerplate()
    {
        Assert.Equal("[vimeo] 76979871: Got HTTP Error 403", YtDlpMessages.LastError(Stderr));
    }

    [Fact]
    public void LastError_NoErrors_IsNull()
    {
        Assert.Null(YtDlpMessages.LastError("[youtube] Extracting URL\n"));
    }

    [Fact]
    public void Warnings_AreDistinctAndInOrder()
    {
        Assert.Equal(["[youtube:tab] YouTube Music is not directly supported."], YtDlpMessages.Warnings(Stderr));
    }
}
