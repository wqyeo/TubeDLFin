using Jellyfin.Plugin.TubeDLFin.Libraries;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Libraries;

public class JellyfinLibraryNotifierTests
{
    private const string File = "/media/movies/YouTube/jawed/Me at the zoo [jNQXAC9IVRw].mp4";

    private readonly ILibraryManager _libraryManager = Substitute.For<ILibraryManager>();
    private readonly JellyfinLibraryNotifier _notifier;

    public JellyfinLibraryNotifierTests()
    {
        _notifier = new JellyfinLibraryNotifier(_libraryManager, NullLogger<JellyfinLibraryNotifier>.Instance);
    }

    [Fact]
    public void FileAdded_RefreshesNearestKnownFolder()
    {
        var youTube = Substitute.For<Folder>();
        _libraryManager.FindByPath("/media/movies/YouTube", true).Returns(youTube);

        _notifier.FileAdded(DownloadPlans.Movies, File);

        youTube.Received(1).ChangedExternally();
        _libraryManager.DidNotReceive().FindByPath("/media/movies", Arg.Any<bool?>());
        _libraryManager.DidNotReceive().QueueLibraryScan();
    }

    [Fact]
    public void FileAdded_WalksUpToLibraryFolder()
    {
        var root = Substitute.For<Folder>();
        _libraryManager.FindByPath("/media/movies", true).Returns(root);

        _notifier.FileAdded(DownloadPlans.Movies, File);

        root.Received(1).ChangedExternally();
        _libraryManager.DidNotReceive().FindByPath("/media", Arg.Any<bool?>());
    }

    [Fact]
    public void FileAdded_LibraryNeverScanned_QueuesScan()
    {
        _notifier.FileAdded(DownloadPlans.Movies, File);

        _libraryManager.Received(1).QueueLibraryScan();
        _libraryManager.DidNotReceive().FindByPath("/media", Arg.Any<bool?>());
    }

    [Fact]
    public void FileAdded_OutsideLibrary_QueuesScanWithoutLookingAround()
    {
        _notifier.FileAdded(DownloadPlans.Movies, "/somewhere/else/file.mp4");

        _libraryManager.Received(1).QueueLibraryScan();
        _libraryManager.DidNotReceiveWithAnyArgs().FindByPath(default!, default);
    }
}
