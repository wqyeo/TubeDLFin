using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Libraries;

namespace Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;

internal static class DownloadPlans
{
    public static readonly LibraryInfo Movies = new("f137a2dd21bbc1b99aa5c0f6bf02a805", "Movies", LibraryType.Movies, ["/media/movies"]);
    public static readonly LibraryInfo Music = new("7e64e319657a9516ec78490da03edccb", "Music", LibraryType.Music, ["/media/music", "/mnt/nas/music"]);

    public static DownloadPlan Create(DownloadProfile? profile = null, LibraryInfo? library = null) =>
        new(Urls.Media("https://www.youtube.com/watch?v=jNQXAC9IVRw"), profile ?? new DownloadProfile(), library ?? Movies, "/media/movies");
}
