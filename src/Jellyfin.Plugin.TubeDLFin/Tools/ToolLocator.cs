using System;
using System.IO;
using Jellyfin.Plugin.TubeDLFin.Configuration;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;

namespace Jellyfin.Plugin.TubeDLFin.Tools;

/// <summary>
/// <see cref="IToolLocator"/> based on the admin settings and the plugin's <c>bin</c> folder.
/// </summary>
public sealed class ToolLocator : IToolLocator
{
    private readonly IToolDefinitionRegistry _definitions;
    private readonly IPluginConfigurationProvider _configuration;
    private readonly IPluginPaths _paths;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolLocator"/> class.
    /// </summary>
    /// <param name="definitions">The tool definitions.</param>
    /// <param name="configuration">The admin settings.</param>
    /// <param name="paths">The plugin's folders.</param>
    /// <param name="environment">The runtime environment.</param>
    public ToolLocator(
        IToolDefinitionRegistry definitions,
        IPluginConfigurationProvider configuration,
        IPluginPaths paths,
        IRuntimeEnvironment environment)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Platform = ToolPlatform.FromEnvironment(environment ?? throw new ArgumentNullException(nameof(environment)));
    }

    /// <inheritdoc />
    public ToolPlatform Platform { get; }

    /// <inheritdoc />
    public ToolLocation Locate(ToolKind kind)
    {
        var custom = CustomPath(kind);
        if (!string.IsNullOrWhiteSpace(custom))
        {
            return new ToolLocation(kind, custom.Trim(), IsCustom: true);
        }

        var executable = _definitions.GetDefinition(kind).GetExecutableName(Platform);
        return new ToolLocation(kind, Path.Combine(_paths.BinDirectory, executable), IsCustom: false);
    }

    private string CustomPath(ToolKind kind)
    {
        var settings = _configuration.Current;
        return kind switch
        {
            ToolKind.YtDlp => settings.YtDlpPath,
            ToolKind.Deno => settings.DenoPath,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown tool."),
        };
    }
}
