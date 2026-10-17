using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Api;
using Jellyfin.Plugin.TubeDLFin.Downloads.Naming;
using Jellyfin.Plugin.TubeDLFin.Libraries;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Api;

public class LibrariesControllerTests
{
    [Fact]
    public void GetLibraries_AddsDefaultNamingAndFreeSpace()
    {
        var libraries = Substitute.For<ILibraryCatalog>();
        libraries.GetLibraries().Returns([DownloadPlans.Movies, DownloadPlans.Music]);
        var diskSpace = Substitute.For<IDiskSpaceProvider>();
        diskSpace.GetFreeBytes("/media/music").Returns(1000);
        diskSpace.GetFreeBytes("/mnt/nas/music").Returns((long?)null);

        var result = new LibrariesController(libraries, Naming.Catalog(), diskSpace).GetLibraries();

        var views = Assert.IsType<List<LibraryView>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(NamingPresetId.MovieFolder, views[0].DefaultNaming);
        Assert.Equal(NamingPresetId.Music, views[1].DefaultNaming);
        Assert.Equal([new LibraryFolderView("/media/music", 1000), new LibraryFolderView("/mnt/nas/music", null)], views[1].Folders);
    }
}
