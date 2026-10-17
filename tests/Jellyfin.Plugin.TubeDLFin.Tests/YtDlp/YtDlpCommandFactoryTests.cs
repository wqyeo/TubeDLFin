using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Configuration;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Jellyfin.Plugin.TubeDLFin.Tools.Status;
using Jellyfin.Plugin.TubeDLFin.YtDlp;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.YtDlp;

public class YtDlpCommandFactoryTests
{
    private static readonly MediaUrl Url = Urls.Media("https://www.youtube.com/watch?v=jNQXAC9IVRw");

    private readonly IToolStatusService _tools = Substitute.For<IToolStatusService>();
    private readonly IFfmpegLocator _ffmpeg = Substitute.For<IFfmpegLocator>();
    private readonly IPluginPaths _paths = Substitute.For<IPluginPaths>();
    private readonly IPluginConfigurationProvider _configuration = Substitute.For<IPluginConfigurationProvider>();
    private readonly PluginConfiguration _settings = new();
    private readonly YtDlpCommandFactory _factory;

    public YtDlpCommandFactoryTests()
    {
        _paths.CacheDirectory.Returns("/data/tubedlfin/cache");
        SetTool(ToolKind.YtDlp, ToolState.Ready, "/data/tubedlfin/bin/yt-dlp");
        SetTool(ToolKind.Deno, ToolState.Ready, "/data/tubedlfin/bin/deno");
        _ffmpeg.GetStatus().Returns(new FfmpegStatus("/usr/lib/jellyfin-ffmpeg/ffmpeg", null, null));
        _configuration.Current.Returns(_ => _settings);
        _factory = new YtDlpCommandFactory(_tools, _ffmpeg, _paths, _configuration);
    }

    [Fact]
    public async Task CreateAsync_AllToolsReady_BuildsFullCommand()
    {
        var request = await _factory.CreateAsync(["--dump-single-json"], Url, TestContext.Current.CancellationToken);

        Assert.Equal("/data/tubedlfin/bin/yt-dlp", request.FileName);
        Assert.Equal(
            [
                "--ignore-config", "--no-color", "--cache-dir", "/data/tubedlfin/cache",
                "--js-runtimes", "deno:/data/tubedlfin/bin/deno",
                "--ffmpeg-location", "/usr/lib/jellyfin-ffmpeg/ffmpeg",
                "--dump-single-json",
                "--", "https://www.youtube.com/watch?v=jNQXAC9IVRw",
            ],
            request.Arguments);
    }

    [Fact]
    public async Task CreateAsync_DenoBrokenAndNoFfmpeg_LeavesThemOut()
    {
        SetTool(ToolKind.Deno, ToolState.Broken, "/data/tubedlfin/bin/deno");
        _ffmpeg.GetStatus().Returns(new FfmpegStatus(null, null, null));

        var request = await _factory.CreateAsync([], Url, TestContext.Current.CancellationToken);

        Assert.DoesNotContain("--js-runtimes", request.Arguments);
        Assert.DoesNotContain("--ffmpeg-location", request.Arguments);
        Assert.Equal(["--", Url.Value.AbsoluteUri], request.Arguments.TakeLast(2));
    }

    [Fact]
    public async Task CreateAsync_CookiesAndExtraArguments_BeforeUrl()
    {
        using var temp = new TempDirectory();
        _settings.CookiesFile = " " + temp.WriteFile("cookies.txt", "# Netscape HTTP Cookie File") + " ";
        _settings.ExtraArguments = "--limit-rate 5M --proxy \"socks5://host:1080\"";

        var request = await _factory.CreateAsync(["--flat-playlist"], Url, TestContext.Current.CancellationToken);

        Assert.Equal(
            ["--cookies", temp.Combine("cookies.txt"), "--flat-playlist", "--limit-rate", "5M", "--proxy", "socks5://host:1080", "--", Url.Value.AbsoluteUri],
            request.Arguments.SkipWhile(a => a != "--cookies"));
    }

    [Fact]
    public async Task CreateAsync_MissingCookiesFile_Throws()
    {
        _settings.CookiesFile = "/nope/cookies.txt";

        var ex = await Assert.ThrowsAsync<ToolNotReadyException>(() => _factory.CreateAsync([], Url, TestContext.Current.CancellationToken));

        Assert.Contains("/nope/cookies.txt", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateAsync_UnclosedQuoteInExtraArguments_Throws()
    {
        _settings.ExtraArguments = "--proxy \"socks5://host";

        await Assert.ThrowsAsync<ToolNotReadyException>(() => _factory.CreateAsync([], Url, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(ToolState.NotInstalled, "not installed yet")]
    [InlineData(ToolState.Broken, "not working: Exec format error")]
    [InlineData(ToolState.Unsupported, "Exec format error")]
    public async Task CreateAsync_YtDlpNotReady_Throws(ToolState state, string expected)
    {
        SetTool(ToolKind.YtDlp, state, "/data/tubedlfin/bin/yt-dlp", "Exec format error");

        var ex = await Assert.ThrowsAsync<ToolNotReadyException>(() => _factory.CreateAsync([], Url, TestContext.Current.CancellationToken));

        Assert.Contains(expected, ex.Message, StringComparison.Ordinal);
    }

    private void SetTool(ToolKind kind, ToolState state, string path, string? message = null) =>
        _tools.GetStatusAsync(kind, Arg.Any<CancellationToken>())
            .Returns(new ToolStatus(kind, kind.ToString(), state, null, path, IsCustom: false, message));
}
