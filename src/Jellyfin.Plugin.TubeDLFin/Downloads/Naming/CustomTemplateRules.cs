using System;
using System.Linq;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Naming;

/// <summary>
/// Checks a custom yt-dlp output template: it must stay inside the destination folder.
/// </summary>
public static class CustomTemplateRules
{
    /// <summary>
    /// The longest template accepted.
    /// </summary>
    public const int MaxLength = 300;

    /// <summary>
    /// Checks a template.
    /// </summary>
    /// <param name="template">The template.</param>
    /// <returns>What is wrong, or <c>null</c> if it is fine.</returns>
    public static string? Check(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return "Enter a custom file name template, or pick a preset.";
        }

        if (template.Length > MaxLength || template.Any(char.IsControl))
        {
            return $"The custom template must be at most {MaxLength} characters on one line.";
        }

        if (!template.EndsWith(".%(ext)s", StringComparison.Ordinal))
        {
            return "The custom template must end with \".%(ext)s\".";
        }

        var segments = template.Split('/', '\\');
        if (template[0] is '/' or '\\' || (template.Length > 1 && template[1] == ':') || segments.Any(s => s.Trim() == ".."))
        {
            return "The custom template must be a relative path without \"..\".";
        }

        return null;
    }
}
