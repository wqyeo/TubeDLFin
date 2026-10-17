using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Jellyfin.Plugin.TubeDLFin.Downloads.Options;
using Jellyfin.Plugin.TubeDLFin.Processes;
using Jellyfin.Plugin.TubeDLFin.YtDlp;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Output;

/// <summary>
/// <see cref="IYtDlpOutputParser"/> for the marker lines <see cref="ProgressOptions"/> sets up,
/// plus yt-dlp's own error and archive messages. Missing values print as <c>NA</c>.
/// </summary>
public sealed class YtDlpOutputParser : IYtDlpOutputParser
{
    private const string ArchiveMessage = "has already been recorded in the archive";
    private const string DownloadPrefix = "[download] ";

    /// <inheritdoc />
    public YtDlpEvent? Parse(ProcessOutputLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var text = line.Text;
        if (text.StartsWith(DownloadMarkers.Progress, StringComparison.Ordinal))
        {
            return Progress(text[DownloadMarkers.Progress.Length..]);
        }

        if (text.StartsWith(DownloadMarkers.PostProcess, StringComparison.Ordinal))
        {
            var fields = text[DownloadMarkers.PostProcess.Length..].Split('|');
            return fields.Length == 2 ? new PostProcessEvent(fields[0], fields[1]) : null;
        }

        if (text.StartsWith(DownloadMarkers.Start, StringComparison.Ordinal))
        {
            return Json(text[DownloadMarkers.Start.Length..], info =>
                new ItemStartedEvent(
                    info.String("id"),
                    info.String("title"),
                    info.Integer("playlist_index"),
                    info.Integer("n_entries") ?? info.Integer("playlist_count")));
        }

        if (text.StartsWith(DownloadMarkers.Done, StringComparison.Ordinal))
        {
            return Json(text[DownloadMarkers.Done.Length..], info =>
                info.String("filepath") is { } path ? new ItemFinishedEvent(Item(info, path)) : null);
        }

        if (YtDlpMessages.Error(text) is { } error)
        {
            return new ErrorEvent(error);
        }

        if (text.StartsWith(DownloadPrefix, StringComparison.Ordinal) && text.EndsWith(ArchiveMessage, StringComparison.Ordinal))
        {
            return new ItemSkippedEvent(text[DownloadPrefix.Length..]);
        }

        return null;
    }

    private static DownloadedItem Item(JsonElement info, string path)
    {
        var artists = info.Strings("artists").ToList();
        return new DownloadedItem(path)
        {
            Id = info.String("id"),
            Title = info.String("title"),
            Description = info.String("description"),
            UploadDate = DateOnly.TryParseExact(info.String("upload_date"), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null,
            Timestamp = info.Number("timestamp") is { } seconds && seconds is > 0 and < 253402300800 ? DateTimeOffset.FromUnixTimeSeconds((long)seconds) : null,
            Uploader = info.String("uploader") ?? info.String("channel"),
            Duration = info.Number("duration"),
            Extractor = info.String("extractor_key"),
            Artist = artists.Count > 0 ? string.Join(", ", artists) : info.String("artist"),
            Album = info.String("album"),
            Track = info.String("track"),
            ReleaseYear = info.Integer("release_year"),
            PlaylistTitle = info.String("playlist_title"),
            PlaylistIndex = info.Integer("playlist_index"),
        };
    }

    private static ProgressEvent? Progress(string text)
    {
        var fields = text.Split('|');
        if (fields.Length != 8)
        {
            return null;
        }

        return new ProgressEvent(
            fields[0],
            Long(fields[1]),
            Long(fields[2]),
            Long(fields[3]),
            Double(fields[4]),
            Double(fields[5]),
            (int?)Long(fields[6]),
            (int?)Long(fields[7]));
    }

    private static YtDlpEvent? Json(string text, Func<JsonElement, YtDlpEvent?> read)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            return read(document.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static double? Double(string text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) ? value : null;

    private static long? Long(string text) =>
        Double(text) is { } value && value is >= int.MinValue and <= long.MaxValue ? (long)value : null;
}
