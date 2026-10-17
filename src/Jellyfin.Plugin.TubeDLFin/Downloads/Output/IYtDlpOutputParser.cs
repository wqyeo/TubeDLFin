using Jellyfin.Plugin.TubeDLFin.Processes;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Output;

/// <summary>
/// Reads the lines a yt-dlp download prints.
/// </summary>
public interface IYtDlpOutputParser
{
    /// <summary>
    /// Reads one line.
    /// </summary>
    /// <param name="line">The line, from standard output or standard error.</param>
    /// <returns>What it reports, or <c>null</c> for ordinary log lines.</returns>
    YtDlpEvent? Parse(ProcessOutputLine line);
}
