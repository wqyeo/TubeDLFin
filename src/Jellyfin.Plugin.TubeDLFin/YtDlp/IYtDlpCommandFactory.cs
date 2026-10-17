using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Processes;

namespace Jellyfin.Plugin.TubeDLFin.YtDlp;

/// <summary>
/// Builds yt-dlp command lines with the options every run needs: TubeDLFin's own cache folder,
/// Deno for YouTube's JavaScript challenges, and Jellyfin's FFmpeg.
/// </summary>
public interface IYtDlpCommandFactory
{
    /// <summary>
    /// Builds a command that runs yt-dlp on one URL.
    /// </summary>
    /// <param name="options">The options for this run, one entry per argument.</param>
    /// <param name="url">The link. It is placed after <c>--</c>, so it can never be read as an option.</param>
    /// <param name="cancellationToken">Cancels the tool check.</param>
    /// <returns>The command, ready for <see cref="IProcessRunner"/>.</returns>
    /// <exception cref="Tools.ToolNotReadyException">yt-dlp is not installed or not working.</exception>
    Task<ProcessRequest> CreateAsync(IEnumerable<string> options, MediaUrl url, CancellationToken cancellationToken);
}
