using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// Cuts out or marks SponsorBlock segments (sponsors, intros, "like and subscribe", ...).
/// </summary>
public sealed class SponsorBlockOptions : IDownloadOptionSet
{
    /// <summary>
    /// Gets yt-dlp's name for a category.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <returns>The name, e.g. <c>selfpromo</c>.</returns>
    public static string YtDlpName(SponsorBlockCategory category) => category switch
    {
        SponsorBlockCategory.Sponsor => "sponsor",
        SponsorBlockCategory.SelfPromo => "selfpromo",
        SponsorBlockCategory.Interaction => "interaction",
        SponsorBlockCategory.Intro => "intro",
        SponsorBlockCategory.Outro => "outro",
        SponsorBlockCategory.Preview => "preview",
        SponsorBlockCategory.Filler => "filler",
        _ => "music_offtopic",
    };

    /// <inheritdoc />
    public void AddTo(IList<string> arguments, DownloadPlan plan, DownloadLocations locations)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(plan);

        var choice = plan.Profile.SponsorBlock;
        if (choice is null || choice.Mode == SponsorBlockMode.Off || choice.Categories.Count == 0)
        {
            return;
        }

        var categories = string.Join(',', choice.Categories.Distinct().Select(YtDlpName));
        if (choice.Mode == SponsorBlockMode.Remove)
        {
            arguments.Add("--sponsorblock-remove");
            arguments.Add(categories);
        }
        else
        {
            // Marked segments are chapters, which only show up if chapters are embedded.
            arguments.Add("--sponsorblock-mark");
            arguments.Add(categories);
            arguments.Add("--embed-chapters");
        }
    }
}
