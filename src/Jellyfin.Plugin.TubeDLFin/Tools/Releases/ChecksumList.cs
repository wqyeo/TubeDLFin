using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Releases;

/// <summary>
/// A <c>sha256sum</c>-style list: one <c>&lt;hex hash&gt;  &lt;file name&gt;</c> per line,
/// as published by both yt-dlp (<c>SHA2-256SUMS</c>) and Deno (<c>*.sha256sum</c>).
/// </summary>
public sealed class ChecksumList
{
    private readonly Dictionary<string, string> _hashes;

    private ChecksumList(Dictionary<string, string> hashes)
    {
        _hashes = hashes;
    }

    /// <summary>
    /// Parses the list. Lines that don't look like <c>hash  name</c> are ignored.
    /// </summary>
    /// <param name="text">The file's text.</param>
    /// <returns>The parsed list.</returns>
    public static ChecksumList Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            var separator = line.IndexOf(' ', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            var hash = line[..separator];

            // "*name" marks binary mode in sha256sum output.
            var name = line[separator..].TrimStart(' ', '*');
            if (hash.Length == 64 && name.Length > 0)
            {
                hashes[name] = hash;
            }
        }

        return new ChecksumList(hashes);
    }

    /// <summary>
    /// Finds the expected hash of a file.
    /// </summary>
    /// <param name="fileName">The file name as listed.</param>
    /// <returns>The hex SHA-256 as listed (compare case-insensitively), or <c>null</c> if the file isn't listed.</returns>
    public string? Find(string fileName) => _hashes.GetValueOrDefault(fileName);
}
