using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Jellyfin.Plugin.TubeDLFin.Detection;

/// <summary>
/// <see cref="IProbeResultParser"/> for yt-dlp's info JSON. Field names are documented in yt-dlp's
/// README under "OUTPUT TEMPLATE".
/// </summary>
public sealed class ProbeResultParser : IProbeResultParser
{
    // yt-dlp writes "none" when a format has no video or no audio; a missing value means unknown.
    private const string None = "none";

    // The page shows thumbnails 320 pixels wide.
    private const int MinThumbnailWidth = 320;

    // Extractor names of list pages: YoutubeTab, SoundcloudSet, BandcampAlbum, ...
    private static readonly string[] _collectionExtractorSuffixes = ["Tab", "Playlist", "Channel", "Album", "Set", "User", "Series", "Season"];

    /// <inheritdoc />
    public ProbeResult Parse(string json, int entryLimit)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentOutOfRangeException.ThrowIfLessThan(entryLimit, 1);

        using var document = JsonDocument.Parse(json);
        var info = document.RootElement;
        if (info.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"Expected a JSON object from yt-dlp, got {info.ValueKind}.");
        }

        var type = info.String("_type");
        var isList = type is "playlist" or "multi_video";
        var entries = isList ? ParseEntries(info, entryLimit) : [];
        var formats = isList ? [] : ParseFormats(info);
        var playlistCount = info.Integer("playlist_count");

        return new ProbeResult
        {
            Kind = !isList ? ProbeKind.Video : IsChannel(info, entries) ? ProbeKind.Channel : ProbeKind.Playlist,
            Id = info.String("id") ?? string.Empty,
            Title = info.String("title") ?? info.String("id") ?? "Untitled",
            Url = HttpUrl(info.String("webpage_url")) ?? HttpUrl(info.String("original_url")),
            Site = info.String("extractor_key") ?? info.String("extractor"),
            Uploader = info.String("uploader") ?? info.String("channel") ?? CommonUploader(entries),
            ChannelUrl = HttpUrl(info.String("channel_url")) ?? HttpUrl(info.String("uploader_url")),
            Thumbnail = Thumbnail(info),
            Duration = info.Number("duration"),
            UploadDate = Date(info.String("upload_date")),
            Artist = Artist(info),
            Album = info.String("album"),
            Track = info.String("track"),
            ReleaseYear = info.Integer("release_year"),
            VideoFormats = formats.Where(f => f.HasVideo).OrderByDescending(f => f.Height ?? 0).ThenByDescending(f => f.Fps ?? 0).ThenByDescending(f => f.Bitrate ?? 0).ToList(),
            AudioFormats = formats.Where(f => !f.HasVideo).OrderByDescending(f => f.AudioBitrate ?? f.Bitrate ?? 0).ToList(),
            Subtitles = ParseSubtitles(info, "subtitles"),
            AutomaticCaptions = ParseSubtitles(info, "automatic_captions"),
            Entries = entries,
            ItemCount = isList ? playlistCount ?? entries.Count : null,
            IsTruncated = entries.Count >= entryLimit && (playlistCount is null || playlistCount > entries.Count),
        };
    }

    private static List<MediaFormat> ParseFormats(JsonElement info)
    {
        var formats = new List<MediaFormat>();
        foreach (var format in info.Array("formats"))
        {
            var id = format.String("format_id");
            var extension = format.String("ext");
            if (id is null || extension is null || extension == "mhtml")
            {
                continue; // mhtml formats are storyboards: preview images, not media.
            }

            var videoCodec = format.String("vcodec");
            var audioCodec = format.String("acodec");
            var hasVideo = HasStream(videoCodec, format.String("video_ext"));
            var hasAudio = HasStream(audioCodec, format.String("audio_ext"));
            if (!hasVideo && !hasAudio)
            {
                continue;
            }

            var exactSize = format.LongInteger("filesize");
            var approximateSize = format.LongInteger("filesize_approx");
            formats.Add(new MediaFormat
            {
                FormatId = id,
                Extension = extension,
                Note = format.String("format_note"),
                Width = hasVideo ? format.Integer("width") : null,
                Height = hasVideo ? format.Integer("height") : null,
                Fps = hasVideo ? format.Number("fps") : null,
                VideoCodec = hasVideo && videoCodec != None ? videoCodec : null,
                AudioCodec = hasAudio && audioCodec != None ? audioCodec : null,
                Bitrate = format.Number("tbr"),
                AudioBitrate = format.Number("abr") is > 0 and var abr ? abr : null,
                FileSize = exactSize ?? approximateSize,
                IsFileSizeApproximate = exactSize is null && approximateSize is not null,
                DynamicRange = hasVideo ? format.String("dynamic_range") : null,
                Language = format.String("language"),
                Protocol = format.String("protocol"),
                HasVideo = hasVideo,
                HasAudio = hasAudio,
            });
        }

        return formats;
    }

    // The codec says "none" when the stream is absent. If the codec is unknown, fall back to the
    // extension fields, and assume the stream is there if those are missing too.
    private static bool HasStream(string? codec, string? extension) =>
        codec is not null ? codec != None : extension != None;

    private static List<SubtitleTrack> ParseSubtitles(JsonElement info, string property)
    {
        var tracks = info.Properties(property)
            .Where(p => p.Name != "live_chat") // YouTube's chat replay is listed as a subtitle.
            .Select(p => new SubtitleTrack(p.Name, p.Value.ValueKind == JsonValueKind.Array ? p.Value.EnumerateArray().Select(t => t.String("name")).FirstOrDefault(n => n is not null) : null));

        // YouTube marks the language that was actually spoken with "-orig"; the rest are translations.
        return tracks
            .OrderByDescending(t => t.Code.EndsWith("-orig", StringComparison.Ordinal))
            .ThenBy(t => t.Name ?? t.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<PlaylistEntry> ParseEntries(JsonElement info, int entryLimit)
    {
        var entries = new List<PlaylistEntry>();
        foreach (var entry in info.Array("entries").Take(entryLimit))
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue; // yt-dlp lists unavailable items as null.
            }

            var isNestedList = entry.String("_type") == "playlist";
            var nestedCount = entry.Array("entries").Count();
            var playlistCount = entry.Integer("playlist_count");
            var isCollection = isNestedList || IsCollectionExtractor(entry.String("ie_key"));

            entries.Add(new PlaylistEntry
            {
                Index = entries.Count + 1,
                Id = entry.String("id"),
                Title = entry.String("title") ?? entry.String("id") ?? "Untitled",
                Url = HttpUrl(entry.String("url")) ?? HttpUrl(entry.String("webpage_url")),
                Duration = entry.Number("duration"),
                Uploader = entry.String("uploader") ?? entry.String("channel"),
                IsCollection = isCollection,
                ItemCount = playlistCount ?? (isNestedList ? nestedCount : null),
                IsItemCountMinimum = playlistCount is null && isNestedList && nestedCount >= entryLimit,
            });
        }

        return entries;
    }

    private static bool IsCollectionExtractor(string? extractor) =>
        extractor is not null && _collectionExtractorSuffixes.Any(suffix => extractor.EndsWith(suffix, StringComparison.Ordinal));

    // A channel page lists the channel's own uploads (its id is the channel id or @handle), or
    // only its tabs.
    private static bool IsChannel(JsonElement info, List<PlaylistEntry> entries)
    {
        var id = info.String("id");
        var channelId = info.String("channel_id");
        return (id is not null && (id == channelId || id.StartsWith('@')))
            || (entries.Count > 0 && info.Array("entries").All(e => e.String("_type") == "playlist"));
    }

    private static string? CommonUploader(List<PlaylistEntry> entries)
    {
        var uploaders = entries.Select(e => e.Uploader).Distinct(StringComparer.Ordinal).ToList();
        return uploaders.Count == 1 ? uploaders[0] : null;
    }

    private static string? Artist(JsonElement info)
    {
        var artists = info.Strings("artists").ToList();
        return artists.Count > 0 ? string.Join(", ", artists) : info.String("artist");
    }

    // Videos name their best thumbnail. Playlists and channels only list candidates, including
    // banners and full-size originals of several megabytes: pick the smallest that still looks
    // good on the page, and fall back to yt-dlp's preferred (last) one if sizes are unknown.
    private static Uri? Thumbnail(JsonElement info)
    {
        var explicitThumbnail = HttpUrl(info.String("thumbnail"));
        if (explicitThumbnail is not null)
        {
            return explicitThumbnail;
        }

        var candidates = info.Array("thumbnails")
            .Select(t => (Url: HttpUrl(t.String("url")), Width: t.Integer("width"), Height: t.Integer("height")))
            .Where(t => t.Url is not null)
            .ToList();

        var sized = candidates
            .Where(t => t.Width >= MinThumbnailWidth && t.Height > 0 && t.Width <= t.Height * 2)
            .OrderBy(t => t.Width)
            .FirstOrDefault();

        return sized.Url ?? candidates.LastOrDefault().Url;
    }

    private static DateOnly? Date(string? yyyymmdd) =>
        DateOnly.TryParseExact(yyyymmdd, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    // Only web links are passed on, so nothing else (javascript:, file:, ...) reaches the page.
    private static Uri? HttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri
            : null;
}
