using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.Plugin.TubeDLFin.YtDlp;

/// <summary>
/// Splits a command-line string into arguments like a POSIX shell would, without running one:
/// spaces separate, single or double quotes group, and a backslash escapes the next character
/// (except inside single quotes). Nothing is expanded.
/// </summary>
public static class ArgumentSplitter
{
    /// <summary>
    /// Splits the text.
    /// </summary>
    /// <param name="text">The text, e.g. <c>--limit-rate 5M --proxy "socks5://host:1080"</c>.</param>
    /// <returns>The arguments.</returns>
    /// <exception cref="FormatException">A quote is not closed.</exception>
    public static IReadOnlyList<string> Split(string? text)
    {
        var arguments = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return arguments;
        }

        var current = new StringBuilder();
        var inArgument = false;
        char? quote = null;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quote == '\'')
            {
                if (c == '\'')
                {
                    quote = null;
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '\\' && i + 1 < text.Length && (quote is null || text[i + 1] is '"' or '\\'))
            {
                current.Append(text[++i]);
                inArgument = true;
            }
            else if (quote == '"')
            {
                if (c == '"')
                {
                    quote = null;
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c is '"' or '\'')
            {
                quote = c;
                inArgument = true;
            }
            else if (char.IsWhiteSpace(c))
            {
                if (inArgument)
                {
                    arguments.Add(current.ToString());
                    current.Clear();
                    inArgument = false;
                }
            }
            else
            {
                current.Append(c);
                inArgument = true;
            }
        }

        if (quote is not null)
        {
            throw new FormatException($"Unclosed {quote} quote in the extra yt-dlp arguments.");
        }

        if (inArgument)
        {
            arguments.Add(current.ToString());
        }

        return arguments;
    }
}
