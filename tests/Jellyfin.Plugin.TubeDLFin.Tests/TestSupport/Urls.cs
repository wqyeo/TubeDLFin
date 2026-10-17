using System;
using Jellyfin.Plugin.TubeDLFin.YtDlp;

namespace Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;

internal static class Urls
{
    public static MediaUrl Media(string text) =>
        MediaUrl.TryCreate(text, out var url, out var error) ? url : throw new ArgumentException(error, nameof(text));
}
