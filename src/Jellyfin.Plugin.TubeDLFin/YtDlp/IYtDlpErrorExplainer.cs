namespace Jellyfin.Plugin.TubeDLFin.YtDlp;

/// <summary>
/// Turns yt-dlp's and the file system's errors into messages that say how to fix the problem.
/// </summary>
public interface IYtDlpErrorExplainer
{
    /// <summary>
    /// Explains an error.
    /// </summary>
    /// <param name="message">The original error message.</param>
    /// <returns>The explanation followed by the original error, or the original error if there is nothing to add.</returns>
    string Explain(string message);
}
