using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Api;
using Jellyfin.Plugin.TubeDLFin.Detection;
using Jellyfin.Plugin.TubeDLFin.Subscriptions;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using static Jellyfin.Plugin.TubeDLFin.Tests.TestSupport.Subscriptions;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Api;

public class SubscriptionsControllerTests
{
    private static readonly SubscriptionRequest Request = new() { Url = "https://www.youtube.com/@BlenderOfficial", Profile = Profile };

    private readonly ISubscriptionService _service = Substitute.For<ISubscriptionService>();
    private readonly SubscriptionsController _controller;
    private readonly SubscriptionInfo _info = new()
    {
        Id = Guid.Parse("8f1d2a3b-4c5d-4e6f-8a9b-0c1d2e3f4a5b"),
        Name = "Blender",
        Url = new Uri("https://www.youtube.com/@BlenderOfficial/videos"),
        IntervalHours = 6,
        Paused = false,
        CreatedAt = Created,
        NextCheckAt = Created.AddHours(6),
        Downloaded = 0,
        Queued = 0,
        GivenUp = 0,
        Profile = Profile,
    };

    public SubscriptionsControllerTests()
    {
        _controller = new SubscriptionsController(_service);
    }

    [Fact]
    public async Task Add_Created()
    {
        _service.AddAsync(Request, Arg.Any<CancellationToken>()).Returns(SubscriptionResult.Done(_info));

        var result = Assert.IsType<CreatedResult>((await _controller.AddSubscription(Request, TestContext.Current.CancellationToken)).Result);

        Assert.Same(_info, result.Value);
        Assert.Equal("TubeDLFin/Subscriptions/8f1d2a3b4c5d4e6f8a9b0c1d2e3f4a5b", result.Location);
    }

    [Fact]
    public async Task Add_Invalid_Is400()
    {
        _service.AddAsync(Request, Arg.Any<CancellationToken>()).Returns(SubscriptionResult.Invalid(["Enter a link.", "Choose a library."]));

        AssertProblem((await _controller.AddSubscription(Request, TestContext.Current.CancellationToken)).Result, 400, "Enter a link. Choose a library.");
    }

    [Theory]
    [InlineData(ProbeFailure.Rejected, 422)]
    [InlineData(ProbeFailure.TimedOut, 504)]
    public async Task Add_FeedFails_MapsStatus(ProbeFailure reason, int status)
    {
        _service.AddAsync(Request, Arg.Any<CancellationToken>()).ThrowsAsync(new ProbeFailedException(reason, "This link is a single video."));

        AssertProblem((await _controller.AddSubscription(Request, TestContext.Current.CancellationToken)).Result, status, "This link is a single video.");
    }

    [Fact]
    public async Task Add_ToolNotReady_Is503()
    {
        _service.AddAsync(Request, Arg.Any<CancellationToken>()).ThrowsAsync(new ToolNotReadyException("yt-dlp is not installed yet."));

        AssertProblem((await _controller.AddSubscription(Request, TestContext.Current.CancellationToken)).Result, 503, "yt-dlp is not installed yet.");
    }

    [Fact]
    public async Task Update_MapsResult()
    {
        var update = new SubscriptionUpdate { Name = "x" };
        var missing = Guid.NewGuid();
        _service.UpdateAsync(_info.Id, update, Arg.Any<CancellationToken>()).Returns(SubscriptionResult.Done(_info));
        _service.UpdateAsync(missing, update, Arg.Any<CancellationToken>()).Returns(SubscriptionResult.NotFound);

        Assert.Same(_info, Assert.IsType<OkObjectResult>((await _controller.UpdateSubscription(_info.Id, update, TestContext.Current.CancellationToken)).Result).Value);
        Assert.IsType<NotFoundResult>((await _controller.UpdateSubscription(missing, update, TestContext.Current.CancellationToken)).Result);
    }

    [Fact]
    public async Task Delete_204Or404()
    {
        _service.DeleteAsync(_info.Id, Arg.Any<CancellationToken>()).Returns(true);

        Assert.IsType<NoContentResult>(await _controller.DeleteSubscription(_info.Id, TestContext.Current.CancellationToken));
        Assert.IsType<NotFoundResult>(await _controller.DeleteSubscription(Guid.NewGuid(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CheckPauseResume_CallService()
    {
        _service.CheckNowAsync(_info.Id, Arg.Any<CancellationToken>()).Returns(SubscriptionResult.Done(_info));
        _service.SetPausedAsync(_info.Id, true, Arg.Any<CancellationToken>()).Returns(SubscriptionResult.Done(_info with { Paused = true }));
        _service.SetPausedAsync(_info.Id, false, Arg.Any<CancellationToken>()).Returns(SubscriptionResult.Done(_info));

        Assert.IsType<OkObjectResult>((await _controller.CheckSubscription(_info.Id, TestContext.Current.CancellationToken)).Result);
        Assert.Equal(true, (Assert.IsType<OkObjectResult>((await _controller.PauseSubscription(_info.Id, TestContext.Current.CancellationToken)).Result).Value as SubscriptionInfo)?.Paused);
        Assert.IsType<OkObjectResult>((await _controller.ResumeSubscription(_info.Id, TestContext.Current.CancellationToken)).Result);
    }

    private static void AssertProblem(ActionResult? response, int status, string detail)
    {
        var result = Assert.IsType<ObjectResult>(response);
        Assert.Equal(status, result.StatusCode);
        Assert.Equal(detail, Assert.IsType<ProblemDetails>(result.Value).Detail);
    }
}
