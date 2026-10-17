using Jellyfin.Plugin.TubeDLFin.Libraries;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Naming;

/// <summary>
/// The built-in presets. Poster names follow what Jellyfin 12.2 reads: <c>poster.jpg</c> in a
/// movie's own folder, <c>name-thumb.jpg</c> for an episode, <c>name-poster.jpg</c> otherwise.
/// </summary>
public static class NamingPresets
{
    /// <summary>
    /// <c>Channel/Title [id].ext</c>, the default for home videos and mixed libraries.
    /// </summary>
    public static readonly NamingPreset ChannelTitle = new(
        NamingPresetId.ChannelTitle,
        "Channel / Title [id]",
        "Blender/Blender Conference Recap [sO_lhW8av3U].mp4",
        $"{Parts.Channel}/{Parts.TitleAndId}.%(ext)s",
        $"{Parts.Channel}/{Parts.TitleAndId}-poster.%(ext)s",
        [LibraryType.HomeVideos, LibraryType.Mixed]);

    /// <summary>
    /// One folder per video, the default for movie libraries.
    /// </summary>
    public static readonly NamingPreset MovieFolder = new(
        NamingPresetId.MovieFolder,
        "Title [id] (one folder per video)",
        "Blender Conference Recap [sO_lhW8av3U]/Blender Conference Recap [sO_lhW8av3U].mp4",
        $"{Parts.TitleAndId}/{Parts.TitleAndId}.%(ext)s",
        $"{Parts.TitleAndId}/poster.%(ext)s",
        [LibraryType.Movies]);

    /// <summary>
    /// A show per channel and a season per year, the default for shows libraries. The episode
    /// number is the upload date and time (MMDDHHMM), so videos uploaded on the same day stay
    /// apart: Jellyfin merges episodes that share a number.
    /// </summary>
    public static readonly NamingPreset TvShow = new(
        NamingPresetId.TvShow,
        "TV show (Channel / Season year / episode by upload date)",
        "Blender/Season 2026/Blender - S2026E10131445 - Blender Conference Recap [sO_lhW8av3U].mp4",
        $"{Parts.Channel}/Season %(upload_date>%Y|0)s/{Parts.Channel} - {Parts.EpisodeTag} - {Parts.TitleAndId}.%(ext)s",
        $"{Parts.Channel}/Season %(upload_date>%Y|0)s/{Parts.Channel} - {Parts.EpisodeTag} - {Parts.TitleAndId}-thumb.%(ext)s",
        [LibraryType.Shows]);

    /// <summary>
    /// <c>Playlist/01 - Title [id].ext</c>.
    /// </summary>
    public static readonly NamingPreset Playlist = new(
        NamingPresetId.Playlist,
        "Playlist / 01 - Title [id]",
        "Blender Conference 2024/01 - Keynote [VZ5022VaMmA].mp4",
        $"{Parts.PlaylistTitle}/%(playlist_index&{{:02d}} - |)s{Parts.TitleAndId}.%(ext)s",
        $"{Parts.PlaylistTitle}/%(playlist_index&{{:02d}} - |)s{Parts.TitleAndId}-poster.%(ext)s",
        []);

    /// <summary>
    /// <c>Artist/Title [id].ext</c>, the default for music video libraries.
    /// </summary>
    public static readonly NamingPreset MusicVideo = new(
        NamingPresetId.MusicVideo,
        "Artist / Title [id]",
        "Daft Punk/Da Funk [d7woYjGWs6k].mp4",
        $"{Parts.Artist}/{Parts.Track} [%(id)s].%(ext)s",
        $"{Parts.Artist}/{Parts.Track} [%(id)s]-poster.%(ext)s",
        [LibraryType.MusicVideos]);

    /// <summary>
    /// <c>Album artist/Album/01 - Title.ext</c>, the default for music libraries; tracks without
    /// an album go in <c>Singles</c>. Jellyfin reads music from the files' tags (see
    /// <see cref="Options.MusicMetadataOptions"/>); the folders just keep things tidy.
    /// </summary>
    public static readonly NamingPreset Music = new(
        NamingPresetId.Music,
        "Artist / Album / 01 - Title",
        "Daft Punk/Homework/01 - Daftendirekt.m4a",
        $"{Parts.AlbumArtist}/{Parts.Album}/{Parts.TrackPrefix}{Parts.Track}.%(ext)s",
        $"{Parts.AlbumArtist}/{Parts.Album}/{Parts.TrackPrefix}{Parts.Track}-poster.%(ext)s",
        [LibraryType.Music]);

    // Template pieces shared by the presets. Each name is cut to a number of bytes (".120B"), so
    // every folder and file name stays under the usual 255-byte limit without losing the "[id]"
    // or extension after it. yt-dlp's --trim-filenames can't do this: it cuts the whole relative
    // path, folders included, from the end.
    private static class Parts
    {
        public const string Channel = "%(uploader,channel,creator|Unknown).60B";
        public const string Artist = "%(artist,creator,uploader|Unknown).60B";
        public const string Title = "%(title).120B";
        public const string TitleAndId = Title + " [%(id)s]";
        public const string Track = "%(track,title).120B";
        public const string AlbumArtist = "%(album_artist,artist,creator,uploader|Unknown).60B";
        public const string Album = "%(album|Singles).80B";
        public const string PlaylistTitle = "%(playlist_title,uploader|Unknown).80B";

        // "01 - " when the track has a number (music tagging pads it to two digits).
        public const string TrackPrefix = "%(track_number&{} - |)s";

        // Season and episode: S2026E10131445 (year, then month, day, hour, minute of the upload).
        public const string EpisodeTag = "S%(upload_date>%Y|0)sE%(upload_date>%m%d|0000)s%(timestamp>%H%M|0000)s";
    }
}
