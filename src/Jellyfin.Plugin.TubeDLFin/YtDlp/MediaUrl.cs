using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Jellyfin.Plugin.TubeDLFin.YtDlp;

/// <summary>
/// A link the admin entered, checked before it goes anywhere near yt-dlp: an absolute
/// <c>http</c> or <c>https</c> URL without spaces, control characters or a user name.
/// </summary>
public sealed record MediaUrl
{
    /// <summary>
    /// The longest link accepted.
    /// </summary>
    public const int MaxLength = 2048;

    private MediaUrl(Uri value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the link.
    /// </summary>
    public Uri Value { get; }

    /// <summary>
    /// Checks a link entered by the admin.
    /// </summary>
    /// <param name="text">The text entered.</param>
    /// <param name="url">The checked link, if valid.</param>
    /// <param name="error">Why the link was refused, if invalid. Meant for the admin.</param>
    /// <returns>Whether the link is valid.</returns>
    public static bool TryCreate(string? text, [NotNullWhen(true)] out MediaUrl? url, [NotNullWhen(false)] out string? error)
    {
        url = null;
        var trimmed = text?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            error = "Enter a link.";
        }
        else if (trimmed.Length > MaxLength)
        {
            error = $"Links can be at most {MaxLength} characters long.";
        }
        else if (trimmed.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
        {
            error = "Links can't contain spaces or line breaks.";
        }
        else if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || uri.Host.Length == 0)
        {
            error = "Enter a full link starting with https:// or http://.";
        }
        else if (uri.UserInfo.Length > 0)
        {
            error = "Links with a user name or password are not supported.";
        }
        else
        {
            url = new MediaUrl(uri);
            error = null;
        }

        return url is not null;
    }

    /// <inheritdoc />
    public override string ToString() => Value.AbsoluteUri;
}
