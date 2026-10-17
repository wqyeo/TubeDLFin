namespace Jellyfin.Plugin.TubeDLFin.Downloads.Output;

/// <summary>
/// A post-processing step started or finished: merging video and audio, converting audio,
/// moving the file into the library, ...
/// </summary>
/// <param name="Processor">yt-dlp's name for the step, e.g. <c>Merger</c>, <c>ExtractAudio</c>, <c>MoveFiles</c>.</param>
/// <param name="Status"><c>started</c> or <c>finished</c>.</param>
public sealed record PostProcessEvent(string Processor, string Status) : YtDlpEvent;
