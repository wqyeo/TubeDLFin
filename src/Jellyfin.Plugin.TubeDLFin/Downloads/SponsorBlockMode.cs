namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// What to do with segments the SponsorBlock community marked.
/// </summary>
public enum SponsorBlockMode
{
    /// <summary>Leave the video as it is.</summary>
    Off,

    /// <summary>Cut the segments out of the video.</summary>
    Remove,

    /// <summary>Add a chapter for each segment, leaving the video intact.</summary>
    Mark,
}
