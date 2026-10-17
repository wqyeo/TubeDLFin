using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.TubeDLFin.Libraries;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Naming;

/// <summary>
/// <see cref="INamingPresetCatalog"/> over the registered presets.
/// </summary>
public sealed class NamingPresetCatalog : INamingPresetCatalog
{
    private const string ExtensionSuffix = ".%(ext)s";

    /// <summary>
    /// Initializes a new instance of the <see cref="NamingPresetCatalog"/> class.
    /// </summary>
    /// <param name="presets">The presets; the first is the fallback for library types none claims.</param>
    public NamingPresetCatalog(IEnumerable<NamingPreset> presets)
    {
        Presets = presets?.ToList() ?? throw new ArgumentNullException(nameof(presets));
        if (Presets.Count == 0)
        {
            throw new ArgumentException("At least one naming preset is needed.", nameof(presets));
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<NamingPreset> Presets { get; }

    /// <inheritdoc />
    public NamingPreset DefaultFor(LibraryType type) =>
        Presets.FirstOrDefault(preset => preset.DefaultFor.Contains(type)) ?? Presets[0];

    /// <inheritdoc />
    public OutputTemplates Resolve(DownloadProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (profile.Naming == NamingPresetId.Custom && profile.CustomTemplate is { } custom)
        {
            // A custom template's poster goes next to the media file: "name-poster.jpg".
            var stem = custom.EndsWith(ExtensionSuffix, StringComparison.Ordinal) ? custom[..^ExtensionSuffix.Length] : custom;
            return new OutputTemplates(custom, stem + "-poster" + ExtensionSuffix);
        }

        var preset = Presets.FirstOrDefault(p => p.Id == profile.Naming) ?? Presets[0];
        return new OutputTemplates(preset.MediaTemplate, preset.ThumbnailTemplate);
    }
}
