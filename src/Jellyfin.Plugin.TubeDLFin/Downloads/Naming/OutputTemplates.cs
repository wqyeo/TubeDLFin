namespace Jellyfin.Plugin.TubeDLFin.Downloads.Naming;

/// <summary>
/// The yt-dlp output templates of one download.
/// </summary>
/// <param name="Media">The template for the media file; ends with <c>.%(ext)s</c>.</param>
/// <param name="Thumbnail">The template for the poster image.</param>
public sealed record OutputTemplates(string Media, string Thumbnail);
