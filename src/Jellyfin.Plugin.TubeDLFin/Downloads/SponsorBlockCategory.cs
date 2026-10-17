namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// SponsorBlock segment categories.
/// </summary>
public enum SponsorBlockCategory
{
    /// <summary>Paid promotion.</summary>
    Sponsor,

    /// <summary>Unpaid or self promotion: merchandise, Patreon, other channels.</summary>
    SelfPromo,

    /// <summary>Reminders to like, subscribe or follow.</summary>
    Interaction,

    /// <summary>Intro animation or intermission.</summary>
    Intro,

    /// <summary>End cards and credits.</summary>
    Outro,

    /// <summary>Previews and recaps.</summary>
    Preview,

    /// <summary>Tangents and jokes not needed for the main content.</summary>
    Filler,

    /// <summary>Non-music parts of music videos.</summary>
    MusicOffTopic,
}
