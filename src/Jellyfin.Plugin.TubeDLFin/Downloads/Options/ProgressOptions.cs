using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// Makes yt-dlp report progress as marker lines that <see cref="Output.YtDlpOutputParser"/> reads.
/// </summary>
public sealed class ProgressOptions : IDownloadOptionSet
{
    /// <inheritdoc />
    public void AddTo(IList<string> arguments, DownloadPlan plan, DownloadLocations locations)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        // One line per update; --print turns on quiet mode, so turn the log and progress back on.
        arguments.Add("--newline");
        arguments.Add("--progress");
        arguments.Add("--no-quiet");
        arguments.Add("--no-simulate");

        arguments.Add("--progress-template");
        arguments.Add("download:" + DownloadMarkers.Progress
            + "%(progress.status)s|%(progress.downloaded_bytes)s|%(progress.total_bytes)s|%(progress.total_bytes_estimate)s"
            + "|%(progress.speed)s|%(progress.eta)s|%(progress.fragment_index)s|%(progress.fragment_count)s");
        arguments.Add("--progress-template");
        arguments.Add("postprocess:" + DownloadMarkers.PostProcess + "%(progress.postprocessor)s|%(progress.status)s");

        arguments.Add("--print");
        arguments.Add("before_dl:" + DownloadMarkers.Start + "%(.{id,title,playlist_index,n_entries,playlist_count})j");
        arguments.Add("--print");
        arguments.Add("after_move:" + DownloadMarkers.Done
            + "%(.{id,title,filepath,description,upload_date,timestamp,uploader,channel,duration,extractor_key,"
            + "artist,artists,album,track,release_year,playlist_title,playlist_index})j");
    }
}
