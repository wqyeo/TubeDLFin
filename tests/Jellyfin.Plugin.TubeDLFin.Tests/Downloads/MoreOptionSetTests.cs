using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Downloads.Options;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Downloads;

public class MoreOptionSetTests
{
    private static readonly DownloadLocations Locations = new("/tmp/job", null);

    [Fact]
    public void Extras_Defaults_PosterTagsAndChapters()
    {
        Assert.Equal(
            ["--write-thumbnail", "--convert-thumbnails", "jpg", "--embed-metadata", "--embed-chapters"],
            Apply(new ExtrasOptions(), new DownloadProfile()));
    }

    [Fact]
    public void Extras_MusicStyle_EmbeddedCoverOnly()
    {
        var extras = new DownloadExtras { WriteNfo = false, SaveThumbnail = false, EmbedThumbnail = true, EmbedChapters = false };

        Assert.Equal(
            ["--convert-thumbnails", "jpg", "--embed-thumbnail", "--embed-metadata"],
            Apply(new ExtrasOptions(), new DownloadProfile { Extras = extras }));
    }

    [Fact]
    public void Extras_AllOff_Nothing()
    {
        var extras = new DownloadExtras { SaveThumbnail = false, EmbedMetadata = false, EmbedChapters = false };

        Assert.Empty(Apply(new ExtrasOptions(), new DownloadProfile { Extras = extras }));
    }

    [Fact]
    public void Subtitles_NoneByDefault()
    {
        Assert.Empty(Apply(new SubtitleOptions(), new DownloadProfile()));
    }

    [Fact]
    public void Subtitles_SrtFilesAutoAndEmbedded()
    {
        var subtitles = new SubtitleChoice { Languages = "en, de", IncludeAutomatic = true, Embed = true };

        Assert.Equal(
            ["--write-subs", "--write-auto-subs", "--sub-langs", "en,de", "--convert-subs", "srt", "--embed-subs"],
            Apply(new SubtitleOptions(), new DownloadProfile { Subtitles = subtitles }));
    }

    [Fact]
    public void Subtitles_NotEmbeddedInAudio()
    {
        var profile = new DownloadProfile { Mode = DownloadMode.Audio, Subtitles = new SubtitleChoice { Languages = "en", Embed = true } };

        Assert.DoesNotContain("--embed-subs", Apply(new SubtitleOptions(), profile));
    }

    [Fact]
    public void SponsorBlock_Remove()
    {
        var choice = new SponsorBlockChoice { Mode = SponsorBlockMode.Remove, Categories = [SponsorBlockCategory.Sponsor, SponsorBlockCategory.SelfPromo, SponsorBlockCategory.MusicOffTopic, SponsorBlockCategory.Sponsor] };

        Assert.Equal(["--sponsorblock-remove", "sponsor,selfpromo,music_offtopic"], Apply(new SponsorBlockOptions(), new DownloadProfile { SponsorBlock = choice }));
    }

    [Fact]
    public void SponsorBlock_MarkAddsChapters()
    {
        var choice = new SponsorBlockChoice { Mode = SponsorBlockMode.Mark, Categories = [SponsorBlockCategory.Intro, SponsorBlockCategory.Outro] };

        Assert.Equal(["--sponsorblock-mark", "intro,outro", "--embed-chapters"], Apply(new SponsorBlockOptions(), new DownloadProfile { SponsorBlock = choice }));
    }

    [Fact]
    public void SponsorBlock_OffOrNoCategories_Nothing()
    {
        Assert.Empty(Apply(new SponsorBlockOptions(), new DownloadProfile { SponsorBlock = new SponsorBlockChoice { Mode = SponsorBlockMode.Off, Categories = [SponsorBlockCategory.Sponsor] } }));
        Assert.Empty(Apply(new SponsorBlockOptions(), new DownloadProfile { SponsorBlock = new SponsorBlockChoice { Mode = SponsorBlockMode.Remove } }));
    }

    private static List<string> Apply(IDownloadOptionSet set, DownloadProfile profile)
    {
        var arguments = new List<string>();
        set.AddTo(arguments, DownloadPlans.Create(profile), Locations);
        return arguments;
    }
}
