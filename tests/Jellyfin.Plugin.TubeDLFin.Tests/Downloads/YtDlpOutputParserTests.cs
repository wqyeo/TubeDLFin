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
    public void Parse_ItemFinished()
    {
        Assert.Equal(
            new ItemFinishedEvent("sO_lhW8av3U", "Blender Conference 2026 Recap", "/media/homevideos/YouTube/Blender/Blender Conference 2026 Recap [sO_lhW8av3U].mp4"),
            Parse("""[tubedlfin:done]{"id": "sO_lhW8av3U", "title": "Blender Conference 2026 Recap", "filepath": "/media/homevideos/YouTube/Blender/Blender Conference 2026 Recap [sO_lhW8av3U].mp4"}"""));
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
