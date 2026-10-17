namespace Jellyfin.Plugin.TubeDLFin.Detection;

/// <summary>
/// Reads the JSON that <c>yt-dlp -J --flat-playlist</c> prints.
/// </summary>
public interface IProbeResultParser
{
    /// <summary>
    /// Parses yt-dlp's JSON.
    /// </summary>
    /// <param name="json">The JSON document yt-dlp printed.</param>
    /// <param name="entryLimit">The <c>--playlist-items</c> limit the listing used, to tell whether lists were cut short.</param>
    /// <returns>The result. <see cref="ProbeResult.HasPlaylistChoice"/> and <see cref="ProbeResult.Warnings"/> are left empty.</returns>
    /// <exception cref="System.Text.Json.JsonException">The text is not a JSON object.</exception>
    ProbeResult Parse(string json, int entryLimit);
}
