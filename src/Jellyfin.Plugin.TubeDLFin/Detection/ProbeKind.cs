namespace Jellyfin.Plugin.TubeDLFin.Detection;

/// <summary>
/// What a detected link points to.
/// </summary>
public enum ProbeKind
{
    /// <summary>A single video or track.</summary>
    Video,

    /// <summary>A playlist, album or other list of items.</summary>
    Playlist,

    /// <summary>A channel, or one of its tabs (Videos, Shorts, Live, Playlists).</summary>
    Channel,
}
