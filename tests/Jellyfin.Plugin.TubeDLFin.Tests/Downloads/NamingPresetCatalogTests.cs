using System.Linq;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Downloads.Naming;
using Jellyfin.Plugin.TubeDLFin.Libraries;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Downloads;

public class NamingPresetCatalogTests
{
    private readonly NamingPresetCatalog _catalog = Naming.Catalog();

    [Theory]
    [InlineData(LibraryType.Movies, NamingPresetId.MovieFolder)]
    [InlineData(LibraryType.Shows, NamingPresetId.TvShow)]
    [InlineData(LibraryType.Music, NamingPresetId.Music)]
    [InlineData(LibraryType.MusicVideos, NamingPresetId.MusicVideo)]
    [InlineData(LibraryType.HomeVideos, NamingPresetId.ChannelTitle)]
    [InlineData(LibraryType.Mixed, NamingPresetId.ChannelTitle)]
    public void DefaultFor_EachLibraryType(LibraryType type, NamingPresetId expected)
    {
        Assert.Equal(expected, _catalog.DefaultFor(type).Id);
    }

    [Fact]
    public void DefaultFor_UnclaimedType_FallsBackToFirst()
    {
        var catalog = new NamingPresetCatalog([NamingPresets.Playlist, NamingPresets.Music]);

        Assert.Equal(NamingPresetId.Playlist, catalog.DefaultFor(LibraryType.Movies).Id);
    }

    [Fact]
    public void Presets_CapEveryNameFieldInBytes()
    {
        // Free-text fields must carry ".NB" so no folder or file name passes 255 bytes.
        string[] textFields = ["title", "track", "uploader", "channel", "artist", "album", "album_artist", "playlist_title", "creator"];
        Assert.All(_catalog.Presets, preset =>
        {
            foreach (var template in new[] { preset.MediaTemplate, preset.ThumbnailTemplate })
            {
                foreach (Match field in Regex.Matches(template, @"%\((?<name>[a-z_]+)[^)]*\)(?<format>[^a-zA-Z]*[a-zA-Z])"))
                {
                    if (textFields.Contains(field.Groups["name"].Value))
                    {
                        Assert.Matches(@"^\.\d+B$", field.Groups["format"].Value);
                    }
                }
            }
        });
    }

    [Fact]
    public void Presets_AllEndWithExtensionAndAreRelative()
    {
        Assert.All(_catalog.Presets, preset =>
        {
            Assert.Null(CustomTemplateRules.Check(preset.MediaTemplate));
            Assert.Null(CustomTemplateRules.Check(preset.ThumbnailTemplate));
        });
        Assert.Equal(_catalog.Presets.Count, _catalog.Presets.Select(p => p.Id).Distinct().Count());
    }

    [Fact]
    public void TvShow_NumbersEpisodesByUploadDateAndTime()
    {
        Assert.Contains("S%(upload_date>%Y|0)sE%(upload_date>%m%d|0000)s%(timestamp>%H%M|0000)s", NamingPresets.TvShow.MediaTemplate, System.StringComparison.Ordinal);
        Assert.EndsWith("-thumb.%(ext)s", NamingPresets.TvShow.ThumbnailTemplate, System.StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_Preset()
    {
        var templates = _catalog.Resolve(new DownloadProfile { Naming = NamingPresetId.MovieFolder });

        Assert.Equal(new OutputTemplates(NamingPresets.MovieFolder.MediaTemplate, NamingPresets.MovieFolder.ThumbnailTemplate), templates);
    }

    [Fact]
    public void Resolve_Custom_PosterNextToFile()
    {
        var templates = _catalog.Resolve(new DownloadProfile { Naming = NamingPresetId.Custom, CustomTemplate = "%(channel)s/%(title)s.%(ext)s" });

        Assert.Equal(new OutputTemplates("%(channel)s/%(title)s.%(ext)s", "%(channel)s/%(title)s-poster.%(ext)s"), templates);
    }
}
