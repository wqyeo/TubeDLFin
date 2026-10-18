using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Api;
using Jellyfin.Plugin.TubeDLFin.Detection;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Jellyfin.Plugin.TubeDLFin.YtDlp;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Api;

public class ProbeControllerTests
{
    private const string Link = "https://www.youtube.com/watch?v=jNQXAC9IVRw";

    private readonly IMediaProber _prober = Substitute.For<IMediaProber>();
    private readonly ProbeController _controller;

    public ProbeControllerTests()
    {
        _controller = new ProbeController(_prober);
    }

    [Fact]
    public async Task Probe_ValidLink_ReturnsResult()
    {
        var expected = new ProbeResult { Kind = ProbeKind.Video, Id = "jNQXAC9IVRw", Title = "Me at the zoo" };
        _prober.ProbeAsync(Arg.Is<MediaUrl>(u => u.Value.AbsoluteUri == Link), true, 100, Arg.Any<CancellationToken>()).Returns(expected);

        var response = await _controller.Probe(new ProbeRequest { Url = Link, NoPlaylist = true }, TestContext.Current.CancellationToken);

        Assert.Same(expected, response.Value);
    }

    [Fact]
    public async Task Probe_InvalidLink_Is400WithoutRunningYtDlp()
    {
        var response = await _controller.Probe(new ProbeRequest { Url = "not a link" }, TestContext.Current.CancellationToken);

        AssertProblem(response, 400, "can't contain spaces");
        await _prober.DidNotReceiveWithAnyArgs().ProbeAsync(default!, default, default, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(ProbeFailure.Rejected, 422)]
    [InlineData(ProbeFailure.TimedOut, 504)]
    [InlineData(ProbeFailure.UnreadableOutput, 502)]
    public async Task Probe_ProbeFails_MapsReasonToStatus(ProbeFailure reason, int status)
    {
        _prober.ProbeAsync(Arg.Any<MediaUrl>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).ThrowsAsync(new ProbeFailedException(reason, "Video unavailable"));

        var response = await _controller.Probe(new ProbeRequest { Url = Link }, TestContext.Current.CancellationToken);

        AssertProblem(response, status, "Video unavailable");
    }

    [Fact]
    public async Task Probe_ToolNotReady_Is503()
    {
        _prober.ProbeAsync(Arg.Any<MediaUrl>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).ThrowsAsync(new ToolNotReadyException("yt-dlp is not installed yet."));

        var response = await _controller.Probe(new ProbeRequest { Url = Link }, TestContext.Current.CancellationToken);

        AssertProblem(response, 503, "not installed yet");
    }

    [Fact]
    public async Task Probe_ClientGone_Is499()
    {
        using var cancel = new CancellationTokenSource();
        await cancel.CancelAsync();
        _prober.ProbeAsync(Arg.Any<MediaUrl>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException(cancel.Token));

        var response = await _controller.Probe(new ProbeRequest { Url = Link }, cancel.Token);

        Assert.Equal(499, Assert.IsType<StatusCodeResult>(response.Result).StatusCode);
    }

    private static void AssertProblem(ActionResult<ProbeResult> response, int status, string detail)
    {
        var result = Assert.IsType<ObjectResult>(response.Result);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(status, result.StatusCode);
        Assert.Contains(detail, problem.Detail, StringComparison.Ordinal);
    }
}
