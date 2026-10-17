using System;
using Jellyfin.Plugin.TubeDLFin.Platform;
using MediaBrowser.Controller;

namespace Jellyfin.Plugin.TubeDLFin.Status;

/// <summary>
/// Builds <see cref="TubeDLFinInfo"/> from the server host, the plugin paths and the runtime environment.
/// </summary>
public sealed class PluginInfoProvider : IPluginInfoProvider
{
    private readonly IServerApplicationHost _serverHost;
    private readonly IPluginPaths _paths;
    private readonly IRuntimeEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginInfoProvider"/> class.
    /// </summary>
    /// <param name="serverHost">The Jellyfin server host.</param>
    /// <param name="paths">The plugin's folders.</param>
    /// <param name="environment">The runtime environment.</param>
    public PluginInfoProvider(IServerApplicationHost serverHost, IPluginPaths paths, IRuntimeEnvironment environment)
    {
        _serverHost = serverHost ?? throw new ArgumentNullException(nameof(serverHost));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <inheritdoc />
    public TubeDLFinInfo GetInfo() => new()
    {
        PluginVersion = typeof(PluginInfoProvider).Assembly.GetName().Version?.ToString() ?? "unknown",
        ServerVersion = _serverHost.ApplicationVersionString,
        OperatingSystem = _environment.OperatingSystemDescription,
        Architecture = _environment.ProcessArchitecture.ToString(),
        DataPath = _paths.DataRoot,
    };
}
