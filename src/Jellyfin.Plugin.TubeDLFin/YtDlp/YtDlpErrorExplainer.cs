using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.TubeDLFin.YtDlp;

/// <summary>
/// <see cref="IYtDlpErrorExplainer"/> for the problems admins run into most.
/// </summary>
public sealed class YtDlpErrorExplainer : IYtDlpErrorExplainer
{
    private static readonly IReadOnlyList<(string Match, string Explanation)> _explanations =
    [
        ("Read-only file system", "The library folder is read-only. In Docker, mount the media folder without \":ro\"."),
        ("Permission denied", "Jellyfin may not write to the library folder. Check the folder's owner and permissions (PUID/PGID in Docker)."),
        ("Access to the path", "Jellyfin may not write to the library folder. Check the folder's owner and permissions (PUID/PGID in Docker)."),
        ("No space left on device", "The disk is full."),
        ("Sign in to confirm", "YouTube wants this server to sign in (common on VPS and data-centre IP addresses). Set a cookies file under Tools & settings."),
        ("cookies", "Check the cookies file set under Tools & settings."),
        ("HTTP Error 429", "The site is rate-limiting this server. Try again later."),
    ];

    /// <inheritdoc />
    public string Explain(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var explanation = _explanations.FirstOrDefault(e => message.Contains(e.Match, StringComparison.OrdinalIgnoreCase)).Explanation;
        return explanation is null ? message : $"{explanation} ({message})";
    }
}
