using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Tools.Updates;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Status;

/// <summary>
/// Everything the dashboard shows about the tools, returned by <c>GET /TubeDLFin/Tools</c>.
/// </summary>
/// <param name="Platform">The platform id, e.g. <c>linux-arm64-glibc</c>.</param>
/// <param name="Tools">yt-dlp and Deno.</param>
/// <param name="Ffmpeg">Jellyfin's FFmpeg.</param>
/// <param name="IsUpdating">Whether an update run is in progress.</param>
/// <param name="LastUpdate">The last update run since the server started, if any.</param>
public sealed record ToolsOverview(
    string Platform,
    IReadOnlyList<ToolStatus> Tools,
    FfmpegStatus Ffmpeg,
    bool IsUpdating,
    ToolUpdateReport? LastUpdate);
