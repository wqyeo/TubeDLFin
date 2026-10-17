using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Downloads.Naming;
using Jellyfin.Plugin.TubeDLFin.Downloads.Options;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Downloads;

// The rules were checked against yt-dlp 2026.08.19 with --print on a YouTube Music album, a
// regular "Artist - Title (Official Video)" upload and a mixed-artist chart playlist.
public class MusicOptionSetTests
{
    private static readonly DownloadLocations Locations = new("/tmp/job", null);
    private static readonly MusicTagging On = new() { Enabled = true };

    [Fact]
    public void Music_Off_Nothing()
    {
        Assert.Empty(Apply(new MusicMetadataOptions(), new DownloadProfile()));
    }

    [Fact]
    public void Music_Defaults_AllRulesInOrder()
    {
        Assert.Equal(
            [
                "--parse-metadata", "%(artist&\n|)s%(title)s:(?P<artist>.+?) - (?P<track>.+)",
                "--replace-in-metadata", "title,track", MusicMetadataOptions.TitleClutter, string.Empty,
                "--parse-metadata", "%(artist,uploader)s:%(artist)s",
                "--replace-in-metadata", "artist", " - Topic$", string.Empty,
                "--parse-metadata", "%(album,playlist_title|)s:%(album)s",
                "--replace-in-metadata", "album", "^Album - ", string.Empty,
                "--parse-metadata", "%(track_number,playlist_index|)s:(?P<track_number>\\d+)",
                "--replace-in-metadata", "track_number", "^(\\d)$", "0\\1",
                "--parse-metadata", "%(release_year,upload_date>%Y)s:%(meta_date)s",
            ],
            Apply(new MusicMetadataOptions(), new DownloadProfile { Music = On }));
    }

    [Fact]
    public void Music_OptionalRulesOff()
    {
        var music = On with { SplitArtistTitle = false, CleanTitles = false, NumberTracks = false };

        var arguments = Apply(new MusicMetadataOptions(), new DownloadProfile { Music = music });

        Assert.DoesNotContain(arguments, a => a.Contains("(?P<track>", System.StringComparison.Ordinal));
        Assert.DoesNotContain(MusicMetadataOptions.TitleClutter, arguments);
        Assert.DoesNotContain(arguments, a => a.Contains("track_number", System.StringComparison.Ordinal));
        Assert.Contains("%(artist,uploader)s:%(artist)s", arguments);
    }

    [Fact]
    public void Music_PlaylistAsAlbum_VariousArtistsOnlyForPlaylistItems()
    {
        var arguments = Apply(new MusicMetadataOptions(), new DownloadProfile { Music = On with { PlaylistAsAlbum = true } });

        Assert.Contains("%(playlist_title|)s:(?P<album>.+)", arguments);
        Assert.Contains("%(playlist_title&Various Artists|)s:(?P<album_artist>.+)", arguments);
        Assert.DoesNotContain("%(album,playlist_title|)s:%(album)s", arguments);
    }

    [Fact]
    public void Music_PlaylistAsAlbum_IgnoredForSingleVideo()
    {
        var plan = DownloadPlans.Create(new DownloadProfile { Music = On with { PlaylistAsAlbum = true } }) with { NoPlaylist = true };

        var arguments = new List<string>();
        new MusicMetadataOptions().AddTo(arguments, plan, Locations);

        Assert.DoesNotContain(arguments, a => a.Contains("Various Artists", System.StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Rick Astley - Never Gonna Give You Up (Official Video) (4K Remaster)", "Rick Astley - Never Gonna Give You Up")]
    [InlineData("Song [Official Music Video]", "Song")]
    [InlineData("Song (Lyrics)", "Song")]
    [InlineData("Song (Lyric Video)", "Song")]
    [InlineData("Song (Official Visualizer) [HD]", "Song")]
    [InlineData("Song (Live at Wembley)", "Song (Live at Wembley)")]
    [InlineData("Song (feat. Someone)", "Song (feat. Someone)")]
    public void TitleClutter_RemovesOnlyVideoSuffixes(string title, string cleaned)
    {
        // yt-dlp uses Python's re; this pattern means the same in .NET.
        Assert.Equal(cleaned, System.Text.RegularExpressions.Regex.Replace(title, MusicMetadataOptions.TitleClutter, string.Empty));
    }

    [Fact]
    public void SquareCover_OnlyWhenEmbeddingMusicCover()
    {
        var embedded = new DownloadExtras { EmbedThumbnail = true };

        Assert.Equal(
            ["--postprocessor-args", SquareCoverOptions.CropArguments],
            Apply(new SquareCoverOptions(), new DownloadProfile { Music = On, Extras = embedded }));
        Assert.Empty(Apply(new SquareCoverOptions(), new DownloadProfile { Music = On }));
        Assert.Empty(Apply(new SquareCoverOptions(), new DownloadProfile { Music = On with { SquareCover = false }, Extras = embedded }));
        Assert.Empty(Apply(new SquareCoverOptions(), new DownloadProfile { Extras = embedded }));
    }

    [Fact]
    public void MusicPreset_AlbumArtistAlbumTrackNumber()
    {
        Assert.Equal(
            "%(album_artist,artist,creator,uploader|Unknown).60B/%(album|Singles).80B/%(track_number&{} - |)s%(track,title).120B.%(ext)s",
            NamingPresets.Music.MediaTemplate);
    }

    private static List<string> Apply(IDownloadOptionSet set, DownloadProfile profile)
    {
        var arguments = new List<string>();
        set.AddTo(arguments, DownloadPlans.Create(profile), Locations);
        return arguments.ToList();
    }
}
