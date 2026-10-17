using System;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.TubeDLFin.Downloads.Nfo;
using Jellyfin.Plugin.TubeDLFin.Downloads.Output;
using Jellyfin.Plugin.TubeDLFin.Libraries;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Downloads.Nfo;

public class NfoBuilderTests
{
    private static readonly DownloadedItem Item = new("/media/movies/Recap [sO_lhW8av3U]/Recap [sO_lhW8av3U].mp4")
    {
        Id = "sO_lhW8av3U",
        Title = "Blender Conference 2026 Recap",
        Description = "Highlights.\nSee you next year!\u0001",
        UploadDate = new DateOnly(2026, 10, 13),
        Timestamp = new DateTimeOffset(2026, 10, 13, 14, 45, 30, TimeSpan.Zero),
        Uploader = "Blender",
        Duration = 149,
        Extractor = "Youtube",
    };

    [Fact]
    public void Movie_WritesKodiMovieNextToFile()
    {
        var file = Assert.Single(new MovieNfoBuilder().Build(Item));

        Assert.Equal("/media/movies/Recap [sO_lhW8av3U]/Recap [sO_lhW8av3U].nfo", file.Path);
        Assert.True(file.Overwrite);
        Assert.Equal(
            """
            <movie>
              <title>Blender Conference 2026 Recap</title>
              <plot>Highlights.
            See you next year!</plot>
              <premiered>2026-10-13</premiered>
              <year>2026</year>
              <studio>Blender</studio>
              <runtime>2</runtime>
              <uniqueid type="youtube" default="true">sO_lhW8av3U</uniqueid>
              <lockdata>true</lockdata>
            </movie>
            """.ReplaceLineEndings(),
            file.Document.Root!.ToString().ReplaceLineEndings());
    }

    [Theory]
    [InlineData(LibraryType.Movies, true)]
    [InlineData(LibraryType.HomeVideos, true)]
    [InlineData(LibraryType.Mixed, true)]
    [InlineData(LibraryType.Shows, false)]
    [InlineData(LibraryType.Music, false)]
    public void Movie_AppliesTo(LibraryType type, bool expected)
    {
        Assert.Equal(expected, new MovieNfoBuilder().AppliesTo(type));
    }

    [Fact]
    public void MissingValues_AreLeftOut()
    {
        var file = Assert.Single(new MovieNfoBuilder().Build(new DownloadedItem("/media/a.mp4")));

        Assert.Equal("<movie>\n  <lockdata>true</lockdata>\n</movie>".ReplaceLineEndings(), file.Document.Root!.ToString().ReplaceLineEndings());
    }

    [Fact]
    public void Episode_SeasonIsYearAndEpisodeIsDateAndTime()
    {
        var item = Item with { FilePath = "/media/tv/Blender/Season 2026/Blender - S2026E10131445 - Recap [sO_lhW8av3U].mp4" };

        var files = new EpisodeNfoBuilder().Build(item);

        var episode = files[0].Document.Root!;
        Assert.Equal("/media/tv/Blender/Season 2026/Blender - S2026E10131445 - Recap [sO_lhW8av3U].nfo", files[0].Path);
        Assert.Equal("episodedetails", episode.Name.LocalName);
        Assert.Equal(("2026", "10131445", "2026-10-13", "Blender"), ((string?)episode.Element("season"), (string?)episode.Element("episode"), (string?)episode.Element("aired"), (string?)episode.Element("showtitle")));

        // The show's file goes in the channel folder, above "Season 2026", and is written once.
        Assert.Equal(("/media/tv/Blender/tvshow.nfo", false), (files[1].Path, files[1].Overwrite));
        Assert.Equal("<tvshow>\n  <title>Blender</title>\n  <lockdata>true</lockdata>\n</tvshow>".ReplaceLineEndings(), files[1].Document.Root!.ToString().ReplaceLineEndings());
    }

    [Fact]
    public void Episode_WithoutSeasonFolder_ShowIsFileFolder()
    {
        var files = new EpisodeNfoBuilder().Build(Item with { FilePath = "/media/tv/YouTube/Blender/Recap.mp4", Uploader = null });

        Assert.Equal(Path.Combine("/media/tv/YouTube/Blender", "tvshow.nfo"), files[1].Path);
        Assert.Equal("Blender", (string?)files[1].Document.Root!.Element("title"));
    }

    [Fact]
    public void EpisodeNumber_MatchesTvShowFileNames()
    {
        Assert.Equal(10131445, EpisodeNfoBuilder.EpisodeNumber(Item));
        Assert.Equal(10130000, EpisodeNfoBuilder.EpisodeNumber(Item with { Timestamp = null }));
        Assert.Equal(0, EpisodeNfoBuilder.EpisodeNumber(new DownloadedItem("/a.mp4")));
    }

    [Fact]
    public void MusicVideo_UsesTrackArtistAlbum()
    {
        var item = Item with { Title = "Daft Punk - Da Funk (Official Video)", Track = "Da Funk", Artist = "Daft Punk", Album = "Homework", ReleaseYear = 1996 };

        var video = Assert.Single(new MusicVideoNfoBuilder().Build(item)).Document.Root!;

        Assert.Equal("musicvideo", video.Name.LocalName);
        Assert.Equal(("Da Funk", "Daft Punk", "Homework", "1996"), ((string?)video.Element("title"), (string?)video.Element("artist"), (string?)video.Element("album"), (string?)video.Element("year")));
    }

    [Fact]
    public void MusicVideo_ArtistFallsBackToUploader()
    {
        var video = Assert.Single(new MusicVideoNfoBuilder().Build(Item)).Document.Root!;

        Assert.Equal("Blender", (string?)video.Element("artist"));
    }

    [Fact]
    public void Builders_CoverEveryVideoLibraryButNotMusic()
    {
        INfoBuilder[] builders = [new MovieNfoBuilder(), new EpisodeNfoBuilder(), new MusicVideoNfoBuilder()];

        foreach (var type in Enum.GetValues<LibraryType>())
        {
            Assert.Equal(type == LibraryType.Music ? 0 : 1, builders.Count(b => b.AppliesTo(type)));
        }
    }
}
