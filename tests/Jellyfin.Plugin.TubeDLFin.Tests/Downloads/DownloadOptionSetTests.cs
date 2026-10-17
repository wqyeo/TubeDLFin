using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Downloads.Naming;
using Jellyfin.Plugin.TubeDLFin.Downloads.Options;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Downloads;

public class DownloadOptionSetTests
{
    private static readonly DownloadLocations Locations = new("/data/tmp/downloads/job1", "/data/archive/lib.txt");

    [Fact]
    public void Output_PutsPartialFilesInTempAndFinishedFilesInLibrary()
    {
        Assert.Equal(
            [
                "--paths", "/media/movies", "--paths", "temp:/data/tmp/downloads/job1",
                "--output", "%(uploader,channel,creator|Unknown)s/%(title)s [%(id)s].%(ext)s",
                "--output", "thumbnail:%(uploader,channel,creator|Unknown)s/%(title)s [%(id)s]-poster.%(ext)s",
                "--no-mtime", "--windows-filenames", "--trim-filenames", "150",
            ],
            Apply(new OutputOptions(Naming.Catalog()), DownloadPlans.Create()));
    }

    [Fact]
    public void Output_UsesChosenPreset()
    {
        var arguments = Apply(new OutputOptions(Naming.Catalog()), DownloadPlans.Create(new DownloadProfile { Naming = NamingPresetId.MovieFolder }));

        Assert.Contains("%(title)s [%(id)s]/%(title)s [%(id)s].%(ext)s", arguments);
        Assert.Contains("thumbnail:%(title)s [%(id)s]/poster.%(ext)s", arguments);
    }

    [Fact]
    public void Progress_PrintsMarkersTheParserReads()
    {
        var arguments = Apply(new ProgressOptions(), DownloadPlans.Create());

        Assert.Contains("--no-quiet", arguments);
        Assert.Contains(arguments, a => a.StartsWith("download:" + DownloadMarkers.Progress, System.StringComparison.Ordinal));
        Assert.Contains(arguments, a => a.StartsWith("postprocess:" + DownloadMarkers.PostProcess, System.StringComparison.Ordinal));
        Assert.Contains(arguments, a => a.StartsWith("before_dl:" + DownloadMarkers.Start, System.StringComparison.Ordinal));
        Assert.Contains(arguments, a => a.StartsWith("after_move:" + DownloadMarkers.Done, System.StringComparison.Ordinal));
    }

    [Fact]
    public void Archive_UsedUnlessRedownloading()
    {
        Assert.Equal(["--download-archive", "/data/archive/lib.txt"], Apply(new ArchiveOptions(), DownloadPlans.Create()));
        Assert.Empty(Apply(new ArchiveOptions(), DownloadPlans.Create(), Locations with { ArchiveFile = null }));
    }

    [Fact]
    public void Playlist_NoPlaylistAndItems()
    {
        Assert.Empty(Apply(new PlaylistOptions(), DownloadPlans.Create()));
        Assert.Equal(
            ["--no-playlist", "--playlist-items", "1-3,7"],
            Apply(new PlaylistOptions(), DownloadPlans.Create() with { NoPlaylist = true, PlaylistItems = "1-3,7" }));
    }

    [Theory]
    [InlineData(null, null, "bv*+ba/b")]
    [InlineData(1080, null, "bv*[height<=1080]+ba/b[height<=1080]/bv*+ba/b")]
    [InlineData(1080, "137", "137[acodec=none]+ba/137")]
    public void Video_FormatSelector(int? maxHeight, string? formatId, string selector)
    {
        var plan = DownloadPlans.Create(new DownloadProfile { MaxHeight = maxHeight }) with { FormatId = formatId };

        Assert.Equal(["--format", selector], Apply(new VideoFormatOptions(), plan));
    }

    [Fact]
    public void Video_Mp4WithH264()
    {
        var plan = DownloadPlans.Create(new DownloadProfile { Container = VideoContainer.Mp4, PreferH264 = true });

        Assert.Equal(
            ["--format", "bv*+ba/b", "--format-sort", "vcodec:h264,acodec:aac", "--merge-output-format", "mp4", "--remux-video", "mp4"],
            Apply(new VideoFormatOptions(), plan));
    }

    [Fact]
    public void Video_WebmPrefersWebmStreams()
    {
        var plan = DownloadPlans.Create(new DownloadProfile { Container = VideoContainer.Webm });

        Assert.Equal(
            ["--format", "bv*+ba/b", "--format-sort", "vext:webm,aext:webm", "--merge-output-format", "webm", "--remux-video", "webm"],
            Apply(new VideoFormatOptions(), plan));
    }

    [Fact]
    public void Video_IgnoredForAudio()
    {
        Assert.Empty(Apply(new VideoFormatOptions(), DownloadPlans.Create(new DownloadProfile { Mode = DownloadMode.Audio })));
        Assert.Empty(Apply(new AudioFormatOptions(), DownloadPlans.Create(new DownloadProfile { Mode = DownloadMode.Video })));
    }

    [Fact]
    public void Audio_OriginalKeepsCodec()
    {
        var plan = DownloadPlans.Create(new DownloadProfile { Mode = DownloadMode.Audio, AudioQuality = 320 });

        Assert.Equal(["--format", "ba/b", "--extract-audio"], Apply(new AudioFormatOptions(), plan));
    }

    [Theory]
    [InlineData(AudioFormat.Mp3, 192, new[] { "--format", "ba/b", "--extract-audio", "--audio-format", "mp3", "--audio-quality", "192K" })]
    [InlineData(AudioFormat.Opus, null, new[] { "--format", "ba/b", "--extract-audio", "--audio-format", "opus" })]
    [InlineData(AudioFormat.Flac, 320, new[] { "--format", "ba/b", "--extract-audio", "--audio-format", "flac" })]
    [InlineData(AudioFormat.Wav, null, new[] { "--format", "ba/b", "--extract-audio", "--audio-format", "wav" })]
    [InlineData(AudioFormat.M4a, 128, new[] { "--format", "ba/b", "--extract-audio", "--audio-format", "m4a", "--audio-quality", "128K" })]
    public void Audio_Conversion(AudioFormat format, int? quality, string[] expected)
    {
        var plan = DownloadPlans.Create(new DownloadProfile { Mode = DownloadMode.Audio, AudioFormat = format, AudioQuality = quality });

        Assert.Equal(expected, Apply(new AudioFormatOptions(), plan));
    }

    [Fact]
    public void Audio_SpecificFormat()
    {
        var plan = DownloadPlans.Create(new DownloadProfile { Mode = DownloadMode.Audio }) with { FormatId = "251" };

        Assert.Equal(["--format", "251", "--extract-audio"], Apply(new AudioFormatOptions(), plan));
    }

    [Fact]
    public void Builder_AppliesSetsInOrder()
    {
        var builder = new DownloadArgumentsBuilder([new PlaylistOptions(), new ArchiveOptions()]);

        Assert.Equal(
            ["--no-playlist", "--download-archive", "/data/archive/lib.txt"],
            builder.Build(DownloadPlans.Create() with { NoPlaylist = true }, Locations));
    }

    private static List<string> Apply(IDownloadOptionSet set, DownloadPlan plan, DownloadLocations? locations = null)
    {
        var arguments = new List<string>();
        set.AddTo(arguments, plan, locations ?? Locations);
        return arguments;
    }
}
