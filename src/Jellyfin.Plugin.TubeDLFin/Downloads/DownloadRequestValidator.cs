using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.TubeDLFin.Libraries;
using Jellyfin.Plugin.TubeDLFin.YtDlp;

namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// <see cref="IDownloadRequestValidator"/>: every free-form value is matched against an
/// allowlist or a strict pattern, and the destination must stay inside one of the chosen
/// library's folders.
/// </summary>
public sealed partial class DownloadRequestValidator : IDownloadRequestValidator
{
    /// <summary>
    /// The "up to" heights offered for video.
    /// </summary>
    public static readonly IReadOnlyList<int> Heights = [2160, 1440, 1080, 720, 480, 360];

    /// <summary>
    /// The bitrates offered for lossy audio, in kbit/s.
    /// </summary>
    public static readonly IReadOnlyList<int> AudioQualities = [320, 256, 192, 128];

    private const int MaxSubfolderLength = 200;
    private const int MaxTitleLength = 300;

    // Characters that are invalid in file names on Windows or SMB shares, which libraries often live on.
    private static readonly char[] _invalidNameChars = [.. Path.GetInvalidFileNameChars().Union(['<', '>', ':', '"', '|', '?', '*'])];

    /// <inheritdoc />
    public DownloadValidation Validate(DownloadRequest request, IReadOnlyList<LibraryInfo> libraries)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(libraries);

        var errors = new List<string>();

        if (!MediaUrl.TryCreate(request.Url, out var url, out var urlError))
        {
            errors.Add(urlError);
        }

        var formatId = Blank(request.FormatId);
        if (formatId is not null && !FormatIdPattern().IsMatch(formatId))
        {
            errors.Add("The format id is not valid.");
        }

        var playlistItems = Blank(request.PlaylistItems)?.Replace(" ", string.Empty, StringComparison.Ordinal);
        if (playlistItems is not null && (playlistItems.Length > 200 || !PlaylistItemsPattern().IsMatch(playlistItems)))
        {
            errors.Add("Playlist items must look like \"1-10\", \"1,3,5\" or \"20:\".");
        }

        var profile = request.Profile;
        LibraryInfo? library = null;
        string? destination = null;
        if (profile is null)
        {
            errors.Add("Choose the download options.");
        }
        else
        {
            CheckOptions(profile, errors);
            library = libraries.FirstOrDefault(l => string.Equals(l.Id, profile.LibraryId, StringComparison.Ordinal));
            if (library is null)
            {
                errors.Add("Choose a library.");
            }
            else
            {
                destination = Destination(library, profile, errors);
            }
        }

        var title = Blank(request.Title);
        if (errors.Count > 0)
        {
            return new DownloadValidation(null, errors);
        }

        var plan = new DownloadPlan(url!, profile!, library!, destination!)
        {
            NoPlaylist = request.NoPlaylist,
            FormatId = formatId,
            PlaylistItems = playlistItems,
            Redownload = request.Redownload,
            Title = title?.Length > MaxTitleLength ? title[..MaxTitleLength] : title,
        };
        return new DownloadValidation(plan, []);
    }

    private static void CheckOptions(DownloadProfile profile, List<string> errors)
    {
        if (!Enum.IsDefined(profile.Mode) || !Enum.IsDefined(profile.Container) || !Enum.IsDefined(profile.AudioFormat))
        {
            errors.Add("Unknown download option.");
        }

        if (profile.MaxHeight is { } height && !Heights.Contains(height))
        {
            errors.Add($"Video quality must be one of {string.Join(", ", Heights)} or best.");
        }

        if (profile.AudioQuality is { } quality && !AudioQualities.Contains(quality))
        {
            errors.Add($"Audio quality must be one of {string.Join(", ", AudioQualities)} kbit/s or best.");
        }
    }

    // The library folder must be one of the library's, and the subfolder must not lead out of it.
    private static string? Destination(LibraryInfo library, DownloadProfile profile, List<string> errors)
    {
        var folder = library.Folders.FirstOrDefault(f => string.Equals(f, profile.LibraryFolder, StringComparison.Ordinal));
        if (folder is null)
        {
            errors.Add($"Choose one of the folders of the library \"{library.Name}\".");
            return null;
        }

        var subfolder = Blank(profile.Subfolder);
        if (subfolder is null)
        {
            return Path.GetFullPath(folder);
        }

        var segments = subfolder.Split('/', '\\');
        if (subfolder.Length > MaxSubfolderLength
            || Path.IsPathRooted(subfolder)
            || segments.Any(s => s.Trim().Length == 0 || s is "." or ".." || s.IndexOfAny(_invalidNameChars) >= 0 || s != s.Trim() || s.EndsWith('.')))
        {
            errors.Add("The subfolder must be a relative path of plain folder names, e.g. \"YouTube/Talks\".");
            return null;
        }

        var root = Path.GetFullPath(folder);
        var full = Path.GetFullPath(Path.Combine([root, .. segments]));
        if (!full.StartsWith(Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            errors.Add("The subfolder must stay inside the library folder.");
            return null;
        }

        return full;
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // yt-dlp format ids: "137", "251-drc", "hls_aac_160k", "dash-video=1080".
    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._=-]{0,79}$", RegexOptions.CultureInvariant)]
    private static partial Regex FormatIdPattern();

    // yt-dlp's --playlist-items: comma-separated items, each "N", "N-M" or a slice "N:M:S" (negative counts from the end).
    [GeneratedRegex(@"^(?:-?\d+(?:--?\d+)?|-?\d*:-?\d*(?::-?\d+)?)(?:,(?:-?\d+(?:--?\d+)?|-?\d*:-?\d*(?::-?\d+)?))*$", RegexOptions.CultureInvariant)]
    private static partial Regex PlaylistItemsPattern();
}
