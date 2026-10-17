using Jellyfin.Plugin.TubeDLFin.Downloads.Output;
using Jellyfin.Plugin.TubeDLFin.Processes;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Downloads;

// Lines are copied from real yt-dlp 2026.08.19 runs with TubeDLFin's options.
public class YtDlpOutputParserTests
{
    private readonly YtDlpOutputParser _parser = new();

    [Fact]
    public void Parse_Progress()
    {
        var progress = Assert.IsType<ProgressEvent>(Parse("[tubedlfin:progress]downloading|7168|223779|NA|2050942.838665666|0|NA|NA"));

        Assert.Equal(new ProgressEvent("downloading", 7168, 223779, null, 2050942.838665666, 0, null, null), progress);
        Assert.Equal(7168.0 / 223779, progress.Fraction!.Value, 6);
    }

    [Fact]
    public void Parse_FragmentedProgress_UsesFragmentsWhenSizeUnknown()
    {
        var progress = Assert.IsType<ProgressEvent>(Parse("[tubedlfin:progress]downloading|1048576|NA|NA|NA|NA|3|12"));

        Assert.Equal(0.25, progress.Fraction);
    }

    [Fact]
    public void Parse_FinishedProgress_IsWhole()
    {
        Assert.Equal(1, Assert.IsType<ProgressEvent>(Parse("[tubedlfin:progress]finished|252182|252182|NA|9389673.77|NA|NA|NA")).Fraction);
    }

    [Fact]
    public void Parse_PostProcess_FromStandardError()
    {
        Assert.Equal(new PostProcessEvent("Merger", "started"), Parse("[tubedlfin:postprocess]Merger|started", isError: true));
    }

    [Fact]
    public void Parse_ItemStarted_InPlaylist()
    {
        var started = Parse("""[tubedlfin:start]{"id": "VZ5022VaMmA", "title": "Keynote — Blender Conference 2024", "playlist_index": 1, "n_entries": 2, "playlist_count": 87}""");

        Assert.Equal(new ItemStartedEvent("VZ5022VaMmA", "Keynote — Blender Conference 2024", 1, 2), started);
    }

    [Fact]
    public void Parse_ItemStarted_SingleVideo()
    {
        Assert.Equal(
            new ItemStartedEvent("jNQXAC9IVRw", "Me at the zoo", null, null),
            Parse("""[tubedlfin:start]{"id": "jNQXAC9IVRw", "title": "Me at the zoo"}"""));
    }

    [Fact]
    public void Parse_ItemFinished_WithMetadata()
    {
        const string Line =
            """[tubedlfin:done]{"id": "d7woYjGWs6k", "title": "Da Funk", "filepath": "/media/music/Daft Punk/Homework/Da Funk.m4a", "description": "Provided to YouTube by Parlophone\n\nDa Funk", "upload_date": "20170209", "timestamp": 1486646534, "uploader": "Daft Punk", "duration": 329, "extractor_key": "Youtube", "artist": "Daft Punk", "artists": ["Daft Punk"], "album": "Homework", "track": "Da Funk", "release_year": 1996, "playlist_title": "Homework", "playlist_index": 7}""";

        var finished = Assert.IsType<ItemFinishedEvent>(Parse(Line));

        Assert.Equal(
            new DownloadedItem("/media/music/Daft Punk/Homework/Da Funk.m4a")
            {
                Id = "d7woYjGWs6k",
                Title = "Da Funk",
                Description = "Provided to YouTube by Parlophone\n\nDa Funk",
                UploadDate = new System.DateOnly(2017, 2, 9),
                Timestamp = System.DateTimeOffset.FromUnixTimeSeconds(1486646534),
                Uploader = "Daft Punk",
                Duration = 329,
                Extractor = "Youtube",
                Artist = "Daft Punk",
                Album = "Homework",
                Track = "Da Funk",
                ReleaseYear = 1996,
                PlaylistTitle = "Homework",
                PlaylistIndex = 7,
            },
            finished.Item);
    }

    [Fact]
    public void Parse_ItemFinished_OnlyPath()
    {
        var finished = Assert.IsType<ItemFinishedEvent>(Parse("""[tubedlfin:done]{"id": "x", "filepath": "/media/a.mp4", "upload_date": "bad"}"""));

        Assert.Equal(new DownloadedItem("/media/a.mp4") { Id = "x" }, finished.Item);
    }

    [Fact]
    public void Parse_Error_WithoutBoilerplate()
    {
        Assert.Equal(
            new ErrorEvent("[vimeo] 76979871: Got HTTP Error 403"),
            Parse("ERROR: [vimeo] 76979871: Got HTTP Error 403; please report this issue on  https://github.com/yt-dlp/yt-dlp/issues?q= , filling out the template", isError: true));
    }

    [Fact]
    public void Parse_ArchiveSkip()
    {
        Assert.Equal(
            new ItemSkippedEvent("VZ5022VaMmA: Keynote — Blender Conference 2024 has already been recorded in the archive"),
            Parse("[download] VZ5022VaMmA: Keynote — Blender Conference 2024 has already been recorded in the archive"));
    }

    [Theory]
    [InlineData("[youtube] sO_lhW8av3U: Downloading webpage")]
    [InlineData("[download] Destination: /config/data/tubedlfin/tmp/downloads/x/a.f396.mp4")]
    [InlineData("WARNING: [youtube] Some formats are missing")]
    [InlineData("[tubedlfin:progress]downloading|1|2")]
    [InlineData("[tubedlfin:start]{not json")]
    [InlineData("""[tubedlfin:done]{"id": "x"}""")]
    [InlineData("")]
    public void Parse_OtherLines_AreNotEvents(string line)
    {
        Assert.Null(Parse(line));
    }

    private YtDlpEvent? Parse(string text, bool isError = false) => _parser.Parse(new ProcessOutputLine(text, isError));
}
