using System;
using System.IO;

namespace Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;

/// <summary>
/// The yt-dlp output samples in Detection/Samples (see the README there).
/// </summary>
internal static class Samples
{
    public static string Read(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Detection", "Samples", name + ".json"));
}
