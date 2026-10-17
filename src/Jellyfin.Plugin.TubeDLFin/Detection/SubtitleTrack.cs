namespace Jellyfin.Plugin.TubeDLFin.Detection;

/// <summary>
/// A subtitle language that can be downloaded.
/// </summary>
/// <param name="Code">yt-dlp's language code, as used with <c>--sub-langs</c>, e.g. <c>en</c> or <c>en-orig</c>.</param>
/// <param name="Name">The language's display name, if the site gave one.</param>
public sealed record SubtitleTrack(string Code, string? Name);
