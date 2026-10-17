namespace Jellyfin.Plugin.TubeDLFin.Libraries;

/// <summary>
/// The kinds of Jellyfin library TubeDLFin can download into. The type decides the defaults
/// (for example audio for music libraries).
/// </summary>
public enum LibraryType
{
    /// <summary>A "Movies" library.</summary>
    Movies,

    /// <summary>A "Shows" library.</summary>
    Shows,

    /// <summary>A "Music" library.</summary>
    Music,

    /// <summary>A "Music Videos" library.</summary>
    MusicVideos,

    /// <summary>A "Home Videos and Photos" library.</summary>
    HomeVideos,

    /// <summary>A "Mixed Movies and Shows" library.</summary>
    Mixed,
}
