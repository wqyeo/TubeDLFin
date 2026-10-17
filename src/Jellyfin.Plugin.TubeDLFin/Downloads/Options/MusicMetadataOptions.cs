using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// Fills in music tags (artist, title, album, track number, year) with yt-dlp's
/// <c>--parse-metadata</c> and <c>--replace-in-metadata</c>. They run before file naming, tag
/// embedding and the <c>.nfo</c>, which all see the result. Rules run in the order given.
/// </summary>
public sealed class MusicMetadataOptions : IDownloadOptionSet
{
    /// <summary>
    /// The suffixes removed from titles: "(Official Video)", "[Official Music Video]",
    /// "(Lyrics)", "(Audio)", "(4K Remaster)", "(Visualizer)", "(HD)", "(MV)", ...
    /// </summary>
    public const string TitleClutter =
        @"(?i)\s*[\(\[](official\s+(music\s+)?(video|audio|visuali[sz]er|lyric\s+video)|lyrics?(\s+video)?|audio|visuali[sz]er|hd|hq|4k(\s+remaster(ed)?)?|remaster(ed)?|mv|m/v)[\)\]]";

    /// <inheritdoc />
    public void AddTo(IList<string> arguments, DownloadPlan plan, DownloadLocations locations)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(plan);

        var music = plan.Profile.Music;
        if (music is null || !music.Enabled)
        {
            return;
        }

        if (music.SplitArtistTitle)
        {
            // Only when the site gave no artist (YouTube Music always does, and its titles can
            // contain " - " as in "Song - Live"): an existing artist puts a line break in front,
            // which the pattern can't match across.
            Parse(arguments, "%(artist&\n|)s%(title)s", "(?P<artist>.+?) - (?P<track>.+)");
        }

        if (music.CleanTitles)
        {
            Replace(arguments, "title,track", TitleClutter, string.Empty);
        }

        // No artist from the site or the title: the channel, without YouTube's " - Topic".
        Parse(arguments, "%(artist,uploader)s", "%(artist)s");
        Replace(arguments, "artist", " - Topic$", string.Empty);

        if (music.PlaylistAsAlbum && !plan.NoPlaylist)
        {
            // Only items of a playlist: a single video has no playlist title, and stays as it is.
            Parse(arguments, "%(playlist_title|)s", "(?P<album>.+)");
            Parse(arguments, "%(playlist_title&Various Artists|)s", "(?P<album_artist>.+)");
        }
        else
        {
            Parse(arguments, "%(album,playlist_title|)s", "%(album)s");
        }

        // YouTube Music names album playlists "Album - Name".
        Replace(arguments, "album", "^Album - ", string.Empty);

        if (music.NumberTracks)
        {
            Parse(arguments, "%(track_number,playlist_index|)s", @"(?P<track_number>\d+)");
            Replace(arguments, "track_number", @"^(\d)$", @"0\1");
        }

        // Album releases have a release year; otherwise the upload year.
        Parse(arguments, "%(release_year,upload_date>%Y)s", "%(meta_date)s");
    }

    private static void Parse(IList<string> arguments, string from, string to)
    {
        arguments.Add("--parse-metadata");
        arguments.Add(from + ":" + to);
    }

    private static void Replace(IList<string> arguments, string fields, string pattern, string replacement)
    {
        arguments.Add("--replace-in-metadata");
        arguments.Add(fields);
        arguments.Add(pattern);
        arguments.Add(replacement);
    }
}
