using Jellyfin.Plugin.TubeDLFin.Subscriptions;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Subscriptions;

public class YouTubeFeedResolverTests
{
    private readonly YouTubeFeedResolver _resolver = new();

    [Theory]
    [InlineData("https://www.youtube.com/@BlenderOfficial", "https://www.youtube.com/@BlenderOfficial/videos")]
    [InlineData("https://youtube.com/@BlenderOfficial/featured", "https://www.youtube.com/@BlenderOfficial/videos")]
    [InlineData("https://m.youtube.com/@BlenderOfficial/videos?view=0", "https://www.youtube.com/@BlenderOfficial/videos")]
    [InlineData("https://www.youtube.com/@BlenderOfficial/Streams", "https://www.youtube.com/@BlenderOfficial/streams")]
    [InlineData("https://www.youtube.com/@BlenderOfficial/shorts", "https://www.youtube.com/@BlenderOfficial/shorts")]
    [InlineData("https://www.youtube.com/channel/UCSMOQeBJ2RAnuFungnQOxLg", "https://www.youtube.com/channel/UCSMOQeBJ2RAnuFungnQOxLg/videos")]
    [InlineData("https://www.youtube.com/c/BlenderFoundation/videos", "https://www.youtube.com/c/BlenderFoundation/videos")]
    [InlineData("https://www.youtube.com/user/BlenderFoundation", "https://www.youtube.com/user/BlenderFoundation/videos")]
    public void Resolve_ChannelTab_NewestFirst(string link, string feed)
    {
        var result = _resolver.Resolve(Urls.Media(link));

        Assert.Equal((feed, YouTubeFeedResolver.ChannelListLimit, true), (result?.Url.Value.AbsoluteUri, result?.ListLimit, result?.NewestFirst));
    }

    [Theory]
    [InlineData("https://www.youtube.com/playlist?list=PLa1F2ddGya_-Ymw4YlOjqrdQxiRMJql5x")]
    [InlineData("https://www.youtube.com/watch?v=sO_lhW8av3U&list=PLa1F2ddGya_-Ymw4YlOjqrdQxiRMJql5x&index=2")]
    public void Resolve_Playlist_ListedInFull(string link)
    {
        var result = _resolver.Resolve(Urls.Media(link));

        Assert.Equal(
            ("https://www.youtube.com/playlist?list=PLa1F2ddGya_-Ymw4YlOjqrdQxiRMJql5x", SubscriptionFeedLocator.DefaultListLimit, false),
            (result?.Url.Value.AbsoluteUri, result?.ListLimit, result?.NewestFirst));
    }

    [Theory]
    [InlineData("https://www.youtube.com/@BlenderOfficial/playlists")]
    [InlineData("https://www.youtube.com/@BlenderOfficial/community")]
    [InlineData("https://www.youtube.com/watch?v=sO_lhW8av3U")]
    [InlineData("https://www.youtube.com/shorts/abc")]
    [InlineData("https://music.youtube.com/channel/UCSMOQeBJ2RAnuFungnQOxLg")]
    [InlineData("https://vimeo.com/channels/staffpicks")]
    public void Resolve_OtherLinks_Null(string link)
    {
        Assert.Null(_resolver.Resolve(Urls.Media(link)));
    }
}
