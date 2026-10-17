using System;
using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Api;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;
using Jellyfin.Plugin.TubeDLFin.Libraries;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Api;

public class DownloadsControllerTests
{
    private readonly IDownloadManager _downloads = Substitute.For<IDownloadManager>();
    private readonly IDownloadRequestValidator _validator = Substitute.For<IDownloadRequestValidator>();
    private readonly ILibraryCatalog _libraries = Substitute.For<ILibraryCatalog>();
    private readonly DownloadsController _controller;
    private readonly DownloadJobInfo _job = new()
    {
        Id = Guid.NewGuid(),
        Url = new Uri("https://www.youtube.com/watch?v=jNQXAC9IVRw"),
        Source = DownloadSources.Manual,
        LibraryName = "Movies",
        Destination = "/media/movies",
        State = DownloadState.Queued,
        CreatedAt = DateTimeOffset.UnixEpoch,
    };

    public DownloadsControllerTests()
    {
        _libraries.GetLibraries().Returns([DownloadPlans.Movies]);
        _controller = new DownloadsController(_downloads, _validator, _libraries);
    }

    [Fact]
    public void AddDownload_Valid_QueuesManualJob()
    {
        var plan = DownloadPlans.Create();
        var request = new DownloadRequest { Url = "https://www.youtube.com/watch?v=jNQXAC9IVRw" };
        _validator.Validate(request, Arg.Is<IReadOnlyList<LibraryInfo>>(l => l.Count == 1)).Returns(new DownloadValidation(plan, []));
        _downloads.Enqueue(plan, DownloadSources.Manual).Returns(_job);

        var result = Assert.IsType<AcceptedResult>(_controller.AddDownload(request).Result);

        Assert.Same(_job, result.Value);
    }

    [Fact]
    public void AddDownload_Invalid_Is400WithReasons()
    {
        _validator.Validate(Arg.Any<DownloadRequest>(), Arg.Any<IReadOnlyList<LibraryInfo>>()).Returns(new DownloadValidation(null, ["Enter a link.", "Choose a library."]));

        var result = Assert.IsType<ObjectResult>(_controller.AddDownload(new DownloadRequest()).Result);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Enter a link. Choose a library.", Assert.IsType<ProblemDetails>(result.Value).Detail);
        _downloads.DidNotReceiveWithAnyArgs().Enqueue(default!, default!);
    }

    [Fact]
    public void Cancel_UnknownJob_Is404()
    {
        Assert.IsType<NotFoundResult>(_controller.Cancel(Guid.NewGuid()).Result);
    }

    [Fact]
    public void Cancel_ReturnsJob()
    {
        _downloads.Cancel(_job.Id).Returns(_job);

        Assert.Same(_job, Assert.IsType<OkObjectResult>(_controller.Cancel(_job.Id).Result).Value);
    }

    [Theory]
    [InlineData(RemoveResult.Removed, 204)]
    [InlineData(RemoveResult.NotFound, 404)]
    [InlineData(RemoveResult.NotFinished, 409)]
    public void Remove_MapsResult(RemoveResult outcome, int status)
    {
        _downloads.Remove(_job.Id).Returns(outcome);

        Assert.Equal(status, Assert.IsAssignableFrom<IStatusCodeActionResult>(_controller.Remove(_job.Id)).StatusCode);
    }

    [Fact]
    public void GetLog_UnknownJob_Is404()
    {
        var id = Guid.NewGuid();
        _downloads.GetLog(id).Returns((IReadOnlyList<string>?)null);

        Assert.IsType<NotFoundResult>(_controller.GetLog(id).Result);
    }

    [Fact]
    public void ClearFinished_ReportsCount()
    {
        _downloads.ClearFinished().Returns(3);

        Assert.Equal(new ClearFinishedResult(3), Assert.IsType<OkObjectResult>(_controller.ClearFinished().Result).Value);
    }
}
