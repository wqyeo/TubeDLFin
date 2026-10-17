using System;
using System.Linq;
using System.Text.Json;
using Jellyfin.Plugin.TubeDLFin.Detection;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Detection;

public class ProbeResultParserTests
{
    private const int Limit = 100;
    private readonly ProbeResultParser _parser = new();

    [Fact]
    public void Parse_Video_ReadsDetails()
    {
        var result = _parser.Parse(Samples.Read("video"), Limit);

        Assert.Equal(ProbeKind.Video, result.Kind);
        Assert.Equal("jNQXAC9IVRw", result.Id);
        Assert.Equal("Me at the zoo", result.Title);
        Assert.Equal(new Uri("https://www.youtube.com/watch?v=jNQXAC9IVRw"), result.Url);
        Assert.Equal("Youtube", result.Site);
        Assert.Equal("jawed", result.Uploader);
        Assert.Equal(new Uri("https://www.youtube.com/channel/UC4QobU6STFB0P71PMvOGN5A"), result.ChannelUrl);
        Assert.StartsWith("https://i.ytimg.com/vi/jNQXAC9IVRw/", result.Thumbnail?.AbsoluteUri, StringComparison.Ordinal);
        Assert.Equal(19, result.Duration);
        Assert.Equal(new DateOnly(2005, 4, 24), result.UploadDate);
        Assert.Empty(result.Entries);
        Assert.Null(result.ItemCount);
        Assert.False(result.IsTruncated);
    }

    [Fact]
    public void Parse_Video_SplitsVideoAndAudioFormats_BestFirst()
    {
        var result = _parser.Parse(Samples.Read("video"), Limit);

        Assert.All(result.VideoFormats, f => Assert.True(f.HasVideo));
        Assert.All(result.AudioFormats, f => Assert.False(f.HasVideo));
        Assert.Equal(24, result.VideoFormats.Count + result.AudioFormats.Count);

        var best = result.VideoFormats[0];
        Assert.Equal(240, best.Height);
        Assert.Equal(new[] { 240, 144 }, result.VideoFormats.Select(f => f.Height!.Value).Distinct());

        var bestAudio = result.AudioFormats[0];
        Assert.Equal(("140", "m4a", "mp4a.40.2"), (bestAudio.FormatId, bestAudio.Extension, bestAudio.AudioCodec));
        Assert.Equal(309288, bestAudio.FileSize);
        Assert.False(bestAudio.IsFileSizeApproximate);
    }

    [Fact]
    public void Parse_Video_HlsAudioWithUnknownCodec_IsAudio()
    {
        var hls = _parser.Parse(Samples.Read("video"), Limit).AudioFormats.Single(f => f.FormatId == "233");

        Assert.True(hls.HasAudio);
        Assert.Null(hls.AudioCodec);
        Assert.Null(hls.FileSize);
        Assert.False(hls.IsFileSizeApproximate);
        Assert.Equal("m3u8_native", hls.Protocol);
    }

    [Fact]
    public void Parse_VideoOnlyFormat_HasNoAudio()
    {
        var format = _parser.Parse(Samples.Read("video-captions"), Limit).VideoFormats.Single(f => f.FormatId == "313");

        Assert.Equal((3840, 2160, 25.0), (format.Width!.Value, format.Height!.Value, format.Fps!.Value));
        Assert.Equal(("vp9", null), (format.VideoCodec, format.AudioCodec));
        Assert.False(format.HasAudio);
        Assert.Equal("SDR", format.DynamicRange);
        Assert.Equal("2160p", format.Note);
    }

    [Fact]
    public void Parse_Storyboards_AreDropped()
    {
        var result = _parser.Parse(Samples.Read("video-captions"), Limit);

        Assert.DoesNotContain(result.VideoFormats.Concat(result.AudioFormats), f => f.FormatId.StartsWith("sb", StringComparison.Ordinal));
        Assert.Equal(2160, result.VideoFormats[0].Height);
    }

    [Fact]
    public void Parse_Subtitles_UploadedSortedByName()
    {
        var result = _parser.Parse(Samples.Read("video"), Limit);

        Assert.Equal(new[] { new SubtitleTrack("en", "English"), new SubtitleTrack("de", "German") }, result.Subtitles);
        Assert.Equal(new[] { "de", "en" }, result.AutomaticCaptions.Select(t => t.Code));
    }

    [Fact]
    public void Parse_AutomaticCaptions_OriginalLanguageFirst()
    {
        var result = _parser.Parse(Samples.Read("video-captions"), Limit);

        Assert.Empty(result.Subtitles);
        Assert.Equal(
            new[] { "en-orig", "ab", "aa", "af", "ak", "sq" },
            result.AutomaticCaptions.Select(t => t.Code));
        Assert.Equal("English (Original)", result.AutomaticCaptions[0].Name);
    }

    [Fact]
    public void Parse_MusicTrack_ReadsMusicFields()
    {
        var result = _parser.Parse(Samples.Read("music-track"), Limit);

        Assert.Equal(("Daft Punk", "Homework", "Da Funk", 1996), (result.Artist, result.Album, result.Track, result.ReleaseYear!.Value));
    }

    [Fact]
    public void Parse_NonYouTubeAudio_HasOnlyAudioFormats()
    {
        var result = _parser.Parse(Samples.Read("soundcloud"), Limit);

        Assert.Equal((ProbeKind.Video, "Soundcloud", "Forss"), (result.Kind, result.Site, result.Uploader));
        Assert.Empty(result.VideoFormats);
        Assert.Equal(new[] { "hls_aac_160k", "hls_aac_96k" }, result.AudioFormats.Select(f => f.FormatId));
        Assert.Equal(160, result.AudioFormats[0].AudioBitrate);
        Assert.Equal("Forss", result.Artist);
    }

    [Fact]
    public void Parse_Playlist_ListsEntries()
    {
        var result = _parser.Parse(Samples.Read("playlist"), Limit);

        Assert.Equal(ProbeKind.Playlist, result.Kind);
        Assert.Equal("Blender Conference 2024", result.Title);
        Assert.Equal(87, result.ItemCount);
        Assert.False(result.IsTruncated);
        Assert.Empty(result.VideoFormats);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, result.Entries.Select(e => e.Index));

        var first = result.Entries[0];
        Assert.Equal(("VZ5022VaMmA", "Keynote — Blender Conference 2024", 1792.0, "Blender"), (first.Id, first.Title, first.Duration!.Value, first.Uploader));
        Assert.Equal(new Uri("https://www.youtube.com/watch?v=VZ5022VaMmA"), first.Url);
        Assert.False(first.IsCollection);
    }

    [Fact]
    public void Parse_Playlist_LongerThanLimit_IsTruncated()
    {
        var result = _parser.Parse(Samples.Read("playlist"), entryLimit: 3);

        Assert.Equal(3, result.Entries.Count);
        Assert.Equal(87, result.ItemCount);
        Assert.True(result.IsTruncated);
    }

    [Fact]
    public void Parse_Album_TakesUploaderFromTracks()
    {
        var result = _parser.Parse(Samples.Read("album"), Limit);

        Assert.Equal((ProbeKind.Playlist, "Daft Punk", 31), (result.Kind, result.Uploader, result.ItemCount!.Value));
        Assert.EndsWith("/maxresdefault.jpg", result.Thumbnail?.AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_ChannelRoot_ListsTabsAsCollections()
    {
        var result = _parser.Parse(Samples.Read("channel"), Limit);

        Assert.Equal(ProbeKind.Channel, result.Kind);
        Assert.Equal(new[] { "Blender - Videos", "Blender - Live", "Blender - Shorts" }, result.Entries.Select(e => e.Title));
        Assert.All(result.Entries, e => Assert.True(e.IsCollection));
        Assert.Equal(new Uri("https://www.youtube.com/@BlenderOfficial/videos"), result.Entries[0].Url);
        Assert.Equal((33, false), (result.Entries[2].ItemCount!.Value, result.Entries[2].IsItemCountMinimum));
    }

    [Fact]
    public void Parse_ChannelRoot_TabCutAtLimit_CountIsMinimum()
    {
        // The sample keeps 3 items per tab; with a limit of 3, the Videos tab hit the limit.
        var videos = _parser.Parse(Samples.Read("channel"), entryLimit: 3).Entries[0];

        Assert.Equal((3, true), (videos.ItemCount!.Value, videos.IsItemCountMinimum));
    }

    [Fact]
    public void Parse_ChannelTab_IsChannelWithVideos()
    {
        var result = _parser.Parse(Samples.Read("channel-videos"), Limit);

        Assert.Equal((ProbeKind.Channel, "Blender - Videos", "Blender"), (result.Kind, result.Title, result.Uploader));
        Assert.All(result.Entries, e => Assert.False(e.IsCollection));
        Assert.Equal(5, result.ItemCount);
        Assert.False(result.IsTruncated);
    }

    [Fact]
    public void Parse_ChannelTab_UnknownTotalAtLimit_IsTruncated()
    {
        var result = _parser.Parse(Samples.Read("channel-videos"), entryLimit: 5);

        Assert.True(result.IsTruncated);
    }

    [Fact]
    public void Parse_PlaylistsTab_EntriesAreCollections()
    {
        var result = _parser.Parse(Samples.Read("channel-playlists"), Limit);

        Assert.Equal((ProbeKind.Channel, 37), (result.Kind, result.ItemCount!.Value));
        Assert.All(result.Entries, e => Assert.True(e.IsCollection));
        Assert.Equal(new Uri("https://www.youtube.com/playlist?list=PLa1F2ddGya_-I9y1nXALrBqrZNdlUE012"), result.Entries[0].Url);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("\"text\"")]
    [InlineData("{ not json")]
    [InlineData("")]
    public void Parse_NotAnObject_Throws(string json)
    {
        Assert.ThrowsAny<JsonException>(() => _parser.Parse(json, Limit));
    }

    [Fact]
    public void Parse_MinimalVideo_UsesFallbacks()
    {
        var result = _parser.Parse("""{ "id": "x1", "formats": [ { "format_id": "0", "ext": "mp4" } ] }""", Limit);

        Assert.Equal((ProbeKind.Video, "x1", "x1"), (result.Kind, result.Id, result.Title));
        var format = Assert.Single(result.VideoFormats);
        Assert.True(format.HasVideo && format.HasAudio, "A format with unknown codecs is assumed to hold both.");
    }

    [Fact]
    public void Parse_UnsafeOrMissingValues_AreDropped()
    {
        const string Json = """
            {
              "_type": "playlist", "id": "p", "title": "List", "webpage_url": "javascript:alert(1)",
              "thumbnail": "file:///etc/passwd", "playlist_count": "many", "upload_date": "2026-01-01",
              "entries": [ null, { "id": "a", "url": "ftp://example.com/a" }, 42 ]
            }
            """;

        var result = _parser.Parse(Json, Limit);

        Assert.Null(result.Url);
        Assert.Null(result.Thumbnail);
        Assert.Null(result.UploadDate);
        var entry = Assert.Single(result.Entries);
        Assert.Equal((1, "a", (Uri?)null), (entry.Index, entry.Title, entry.Url));
        Assert.Equal(1, result.ItemCount);
    }

    [Theory]
    [InlineData("YoutubeTab", true)]
    [InlineData("SoundcloudSet", true)]
    [InlineData("BandcampAlbum", true)]
    [InlineData("Youtube", false)]
    [InlineData(null, false)]
    public void Parse_EntryExtractor_DecidesCollection(string? extractor, bool isCollection)
    {
        var ieKey = extractor is null ? string.Empty : $", \"ie_key\": \"{extractor}\"";
        var json = $$"""{ "_type": "playlist", "id": "p", "entries": [ { "_type": "url", "id": "a"{{ieKey}} } ] }""";

        Assert.Equal(isCollection, _parser.Parse(json, Limit).Entries[0].IsCollection);
    }
}
