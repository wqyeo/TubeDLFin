using System.Linq;
using Jellyfin.Plugin.TubeDLFin.Libraries;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Libraries;

public class JellyfinLibraryCatalogTests
{
    [Fact]
    public void GetLibraries_MapsTypesAndSkipsOthers()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        libraryManager.GetVirtualFolders().Returns(
        [
            Folder("Shows", CollectionTypeOptions.tvshows, "/media/tv"),
            Folder("Books", CollectionTypeOptions.books, "/media/books"),
            Folder("Mixed", null, "/media/mixed"),
            Folder("Music", CollectionTypeOptions.music, "/media/music", " ", "/mnt/nas/music"),
            Folder("Empty", CollectionTypeOptions.movies),
            Folder("Home", CollectionTypeOptions.homevideos, "/media/home"),
        ]);

        var libraries = new JellyfinLibraryCatalog(libraryManager).GetLibraries();

        Assert.Equal(["Home", "Mixed", "Music", "Shows"], libraries.Select(l => l.Name));
        Assert.Equal([LibraryType.HomeVideos, LibraryType.Mixed, LibraryType.Music, LibraryType.Shows], libraries.Select(l => l.Type));
        Assert.Equal(["/media/music", "/mnt/nas/music"], libraries[2].Folders);
        Assert.Equal("id-Music", libraries[2].Id);
    }

    private static VirtualFolderInfo Folder(string name, CollectionTypeOptions? type, params string[] locations) =>
        new() { Name = name, CollectionType = type, Locations = locations, ItemId = "id-" + name };
}
