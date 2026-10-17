using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Libraries;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Naming;

/// <summary>
/// A file naming scheme: where yt-dlp puts the media file and its poster, relative to the
/// destination folder. Subtitles follow the media file's name (<c>name.en.srt</c>).
/// </summary>
/// <param name="Id">The preset's id.</param>
/// <param name="Name">The name shown to the admin.</param>
/// <param name="Example">What a file name looks like, shown to the admin.</param>
/// <param name="MediaTemplate">The yt-dlp output template for the media file.</param>
/// <param name="ThumbnailTemplate">The yt-dlp output template for the poster image.</param>
/// <param name="DefaultFor">The library types this preset is the default for.</param>
public sealed record NamingPreset(
    NamingPresetId Id,
    string Name,
    string Example,
    string MediaTemplate,
    string ThumbnailTemplate,
    IReadOnlyList<LibraryType> DefaultFor);
