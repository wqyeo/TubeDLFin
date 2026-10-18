using Jellyfin.Plugin.TubeDLFin.Subscriptions;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Subscriptions;

public class SubscriptionFeedLocatorTests
{
    private readonly SubscriptionFeedLocator _locator = new([new YouTubeFeedResolver()]);

    [Fact]
    public void Locate_KnownLink_UsesResolver()
    {
        var feed = _locator.Locate(Urls.Media("https://www.youtube.com/@BlenderOfficial"));

        Assert.Equal("https://www.youtube.com/@BlenderOfficial/videos", feed.Url.Value.AbsoluteUri);
    }

    [Fact]
    public void Locate_UnknownLink_ListedAsGivenInFull()
    {
        var url = Urls.Media("https://soundcloud.com/forss/sets/soulhack");

        Assert.Equal(new SubscriptionFeed(url, SubscriptionFeedLocator.DefaultListLimit, NewestFirst: false), _locator.Locate(url));
    }
}
