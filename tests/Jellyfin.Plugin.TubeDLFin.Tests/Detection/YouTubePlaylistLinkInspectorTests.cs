using Jellyfin.Plugin.TubeDLFin.Detection;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Detection;

public class YouTubePlaylistLinkInspectorTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=sO_lhW8av3U&list=PLa1F2ddGya_-Ymw4YlOjqrdQxiRMJql5x", true)]
    [InlineData("https://music.youtube.com/watch?v=d7woYjGWs6k&list=OLAK5uy_x", true)]
    [InlineData("https://m.youtube.com/watch?list=PL1&v=abc&index=3", true)]
    [InlineData("https://youtu.be/sO_lhW8av3U?list=PL1", true)]
    [InlineData("https://www.youtube.com/watch?v=sO_lhW8av3U", false)]
    [InlineData("https://www.youtube.com/playlist?list=PL1", false)]
    [InlineData("https://www.youtube.com/watch?v=&list=PL1", false)]
    [InlineData("https://youtu.be/?list=PL1", false)]
    [InlineData("https://example.com/watch?v=abc&list=PL1", false)]
    public void HasPlaylistChoice_RecognisesVideoInPlaylistLinks(string url, bool expected)
    {
        Assert.Equal(expected, new YouTubePlaylistLinkInspector().HasPlaylistChoice(Urls.Media(url)));
    }
}
