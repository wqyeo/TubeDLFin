using System.Linq;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Subscriptions;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Xunit;
using static Jellyfin.Plugin.TubeDLFin.Tests.TestSupport.Subscriptions;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Subscriptions;

public class SubscriptionValidatorTests
{
    private const string Link = "https://www.youtube.com/@BlenderOfficial";

    private readonly SubscriptionValidator _validator = new(new DownloadRequestValidator());

    [Fact]
    public void ValidateNew_Defaults()
    {
        var validation = _validator.ValidateNew(new SubscriptionRequest { Url = Link, Name = "  ", Profile = Profile }, [DownloadPlans.Movies]);

        var settings = Assert.IsType<SubscriptionSettings>(validation.Settings);
        Assert.Equal((Link, (string?)null, Subscription.DefaultIntervalHours), (settings.Url.Value.AbsoluteUri, settings.Name, settings.IntervalHours));
    }

    [Fact]
    public void ValidateNew_CollectsEveryError()
    {
        var validation = _validator.ValidateNew(
            new SubscriptionRequest { Url = "ftp://x", Name = new string('n', 201), IntervalHours = 0, Profile = Profile with { LibraryId = "gone" } },
            [DownloadPlans.Movies]);

        Assert.False(validation.IsValid);
        Assert.Equal(4, validation.Errors.Count);
        Assert.Contains("Check every 1 to 168 hours.", validation.Errors);
        Assert.Contains("Choose a library.", validation.Errors);
    }

    [Fact]
    public void ValidateNew_NoProfile_Refused()
    {
        Assert.Contains("Choose the download options.", _validator.ValidateNew(new SubscriptionRequest { Url = Link }, [DownloadPlans.Movies]).Errors);
    }

    [Fact]
    public void ValidateUpdate_KeepsWhatIsLeftOut_EvenIfLibraryIsGone()
    {
        var current = Create() with { IntervalHours = 12 };

        var settings = _validator.ValidateUpdate(current, new SubscriptionUpdate { Name = " Blender Studio " }, []).Settings;

        Assert.Equal(("Blender Studio", 12, Profile), (settings?.Name, settings?.IntervalHours, settings?.Profile));
    }

    [Fact]
    public void ValidateUpdate_NewProfileAndInterval_Checked()
    {
        var current = Create();
        var audio = Profile with { Mode = DownloadMode.Audio };

        var settings = _validator.ValidateUpdate(current, new SubscriptionUpdate { IntervalHours = 24, Profile = audio }, [DownloadPlans.Movies]).Settings;

        Assert.Equal((24, audio), (settings?.IntervalHours, settings?.Profile));
        Assert.Equal(["Enter a name."], _validator.ValidateUpdate(current, new SubscriptionUpdate { Name = string.Empty }, []).Errors);
        Assert.Equal(["Choose a library."], _validator.ValidateUpdate(current, new SubscriptionUpdate { Profile = Profile }, []).Errors.ToList());
    }
}
