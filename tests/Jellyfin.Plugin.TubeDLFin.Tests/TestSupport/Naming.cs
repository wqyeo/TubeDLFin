using Jellyfin.Plugin.TubeDLFin.Downloads.Naming;

namespace Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;

internal static class Naming
{
    public static NamingPresetCatalog Catalog() => new(
    [
        NamingPresets.ChannelTitle,
        NamingPresets.MovieFolder,
        NamingPresets.TvShow,
        NamingPresets.Playlist,
        NamingPresets.MusicVideo,
        NamingPresets.Music,
    ]);
}
