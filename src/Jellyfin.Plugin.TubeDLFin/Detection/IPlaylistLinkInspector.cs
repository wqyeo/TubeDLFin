using Jellyfin.Plugin.TubeDLFin.YtDlp;

namespace Jellyfin.Plugin.TubeDLFin.Detection;

/// <summary>
/// Recognises links that point to a video inside a playlist, such as YouTube's
/// <c>watch?v=…&amp;list=…</c>. yt-dlp opens the whole playlist for these unless it gets
/// <c>--no-playlist</c>, so the admin is offered both. Register one per site.
/// </summary>
public interface IPlaylistLinkInspector
{
    /// <summary>
    /// Checks whether the link names both a video and a playlist.
    /// </summary>
    /// <param name="url">The link.</param>
    /// <returns><c>true</c> if the admin can choose between the video and the playlist.</returns>
    bool HasPlaylistChoice(MediaUrl url);
}
