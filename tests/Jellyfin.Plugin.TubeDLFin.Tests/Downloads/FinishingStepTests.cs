using System;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Downloads.Finishing;
using Jellyfin.Plugin.TubeDLFin.Downloads.Nfo;
using Jellyfin.Plugin.TubeDLFin.Downloads.Output;
using Jellyfin.Plugin.TubeDLFin.Libraries;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Downloads;

public sealed class FinishingStepTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Nfo_WritesUtf8FilesAndKeepsExistingShowFile()
    {
        var media = _temp.WriteFile(Path.Combine("Blender", "Season 2026", "Recap [x].mp4"), "video");
        var show = _temp.WriteFile(Path.Combine("Blender", "tvshow.nfo"), "<tvshow><title>Edited by hand</title></tvshow>");
        var plan = DownloadPlans.Create(library: new LibraryInfo("id", "Shows", LibraryType.Shows, [_temp.Path]));

        new NfoStep([new MovieNfoBuilder(), new EpisodeNfoBuilder()]).Run(plan, new DownloadedItem(media) { Title = "Récap ✓", Uploader = "Blender" });

        var nfo = File.ReadAllText(Path.ChangeExtension(media, ".nfo"));
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>", nfo, StringComparison.Ordinal);
        Assert.Contains("<episodedetails>", nfo, StringComparison.Ordinal);
        Assert.Contains("<title>Récap ✓</title>", nfo, StringComparison.Ordinal);
        Assert.Equal("<tvshow><title>Edited by hand</title></tvshow>", File.ReadAllText(show));
        Assert.Empty(Directory.EnumerateFiles(_temp.Path, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public void Nfo_TurnedOff_WritesNothing()
    {
        var media = _temp.WriteFile("a.mp4", "video");
        var plan = DownloadPlans.Create(new DownloadProfile { Extras = new DownloadExtras { WriteNfo = false } });

        new NfoStep([new MovieNfoBuilder()]).Run(plan, new DownloadedItem(media));

        Assert.False(File.Exists(Path.ChangeExtension(media, ".nfo")));
    }

    [Fact]
    public void Subtitles_OriginalLanguageRenamedToPlainCode()
    {
        var media = _temp.WriteFile("Recap [x].mp4", "video");
        _temp.WriteFile("Recap [x].en-orig.srt", "orig");
        _temp.WriteFile("Recap [x].de-orig.srt", "de orig");
        _temp.WriteFile("Recap [x].de.srt", "uploaded de");
        _temp.WriteFile("Other [y].en-orig.srt", "other video");

        new SubtitleLanguageStep().Run(DownloadPlans.Create(), new DownloadedItem(media));

        var files = Directory.EnumerateFiles(_temp.Path).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(["Other [y].en-orig.srt", "Recap [x].de.orig.srt", "Recap [x].de.srt", "Recap [x].en.srt", "Recap [x].mp4"], files);
        Assert.Equal("orig", File.ReadAllText(_temp.Combine("Recap [x].en.srt")));
        Assert.Equal("uploaded de", File.ReadAllText(_temp.Combine("Recap [x].de.srt")));
        Assert.Equal("de orig", File.ReadAllText(_temp.Combine("Recap [x].de.orig.srt")));
    }
}
