namespace Jellyfin.Plugin.TubeDLFin.Downloads.Naming;

/// <summary>
/// The file naming schemes TubeDLFin offers.
/// </summary>
public enum NamingPresetId
{
    /// <summary><c>Channel/Title [id].ext</c>.</summary>
    ChannelTitle,

    /// <summary><c>Title [id]/Title [id].ext</c>: one folder per video, as Jellyfin expects for movies.</summary>
    MovieFolder,

    /// <summary><c>Channel/Season YYYY/Channel - SYYYYEMMDDHHMM - Title [id].ext</c>, for shows libraries.</summary>
    TvShow,

    /// <summary><c>Playlist/01 - Title [id].ext</c>.</summary>
    Playlist,

    /// <summary><c>Artist/Title [id].ext</c>, for music videos.</summary>
    MusicVideo,

    /// <summary><c>Artist/Album/Title.ext</c>, for music.</summary>
    Music,

    /// <summary>The admin's own yt-dlp output template.</summary>
    Custom,
}
