using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// <see cref="IDownloadArgumentsBuilder"/> applying every registered <see cref="IDownloadOptionSet"/>.
/// </summary>
public sealed class DownloadArgumentsBuilder : IDownloadArgumentsBuilder
{
    private readonly IEnumerable<IDownloadOptionSet> _optionSets;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadArgumentsBuilder"/> class.
    /// </summary>
    /// <param name="optionSets">The option groups, applied in order.</param>
    public DownloadArgumentsBuilder(IEnumerable<IDownloadOptionSet> optionSets)
    {
        _optionSets = optionSets ?? throw new ArgumentNullException(nameof(optionSets));
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Build(DownloadPlan plan, DownloadLocations locations)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(locations);

        var arguments = new List<string>();
        foreach (var optionSet in _optionSets)
        {
            optionSet.AddTo(arguments, plan, locations);
        }

        return arguments;
    }
}
