using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.TubeDLFin.YtDlp;

/// <summary>
/// Reads the <c>ERROR:</c> and <c>WARNING:</c> lines yt-dlp writes to standard error.
/// </summary>
public static class YtDlpMessages
{
    private const string ErrorPrefix = "ERROR: ";
    private const string WarningPrefix = "WARNING: ";

    // yt-dlp appends this to many errors. It's addressed to yt-dlp users, not to TubeDLFin's admins.
    private const string ReportSuffix = "; please report this issue on";

    /// <summary>
    /// Finds the last error yt-dlp reported, which is the one that stopped it.
    /// </summary>
    /// <param name="standardError">yt-dlp's standard error.</param>
    /// <returns>The error without its prefix, or <c>null</c> if there is none.</returns>
    public static string? LastError(string standardError) =>
        Lines(standardError, ErrorPrefix).LastOrDefault();

    /// <summary>
    /// Reads one line as an error.
    /// </summary>
    /// <param name="line">A line yt-dlp printed.</param>
    /// <returns>The error without its prefix, or <c>null</c> if the line isn't an error.</returns>
    public static string? Error(string line) => Message(line, ErrorPrefix);

    /// <summary>
    /// Finds every warning yt-dlp reported, without duplicates.
    /// </summary>
    /// <param name="standardError">yt-dlp's standard error.</param>
    /// <returns>The warnings without their prefix, in order.</returns>
    public static IReadOnlyList<string> Warnings(string standardError) =>
        Lines(standardError, WarningPrefix).Distinct(StringComparer.Ordinal).ToList();

    private static IEnumerable<string> Lines(string text, string prefix)
    {
        ArgumentNullException.ThrowIfNull(text);

        foreach (var line in text.Split('\n'))
        {
            if (Message(line, prefix) is { } message)
            {
                yield return message;
            }
        }
    }

    private static string? Message(string line, string prefix)
    {
        ArgumentNullException.ThrowIfNull(line);

        line = line.TrimEnd('\r');
        if (!line.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var message = line[prefix.Length..];
        var suffix = message.IndexOf(ReportSuffix, StringComparison.Ordinal);
        return (suffix < 0 ? message : message[..suffix]).Trim();
    }
}
