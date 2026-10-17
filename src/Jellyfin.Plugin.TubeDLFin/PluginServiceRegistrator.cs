using System;
using Jellyfin.Plugin.TubeDLFin.Configuration;
using Jellyfin.Plugin.TubeDLFin.Detection;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;
using Jellyfin.Plugin.TubeDLFin.Downloads.Options;
using Jellyfin.Plugin.TubeDLFin.Downloads.Output;
using Jellyfin.Plugin.TubeDLFin.Libraries;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Processes;
using Jellyfin.Plugin.TubeDLFin.Status;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;
using Jellyfin.Plugin.TubeDLFin.Tools.Installation;
using Jellyfin.Plugin.TubeDLFin.Tools.Releases;
using Jellyfin.Plugin.TubeDLFin.Tools.Status;
using Jellyfin.Plugin.TubeDLFin.Tools.Updates;
using Jellyfin.Plugin.TubeDLFin.YtDlp;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jellyfin.Plugin.TubeDLFin;

/// <summary>
/// Registers the plugin's services with Jellyfin's dependency injection container.
/// This is the plugin's composition root: classes depend on interfaces, and this is the
/// only place that picks the implementations.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    // Each detection is a yt-dlp process; keep a Raspberry Pi responsive.
    private const int MaxConcurrentProbes = 2;

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Platform and settings
        serviceCollection.TryAddSingleton(TimeProvider.System);
        serviceCollection.AddSingleton<IRuntimeEnvironment, RuntimeEnvironment>();
        serviceCollection.AddSingleton<IPluginPaths, PluginPaths>();
        serviceCollection.AddSingleton<IPluginConfigurationProvider, PluginConfigurationProvider>();
        serviceCollection.AddSingleton<IProcessRunner, ProcessRunner>();
        serviceCollection.AddSingleton<IPluginInfoProvider, PluginInfoProvider>();

        // Tools: add a tool by registering another IToolDefinition.
        serviceCollection.AddSingleton<IToolDefinition, YtDlpToolDefinition>();
        serviceCollection.AddSingleton<IToolDefinition, DenoToolDefinition>();
        serviceCollection.AddSingleton<IToolDefinitionRegistry, ToolDefinitionRegistry>();
        serviceCollection.AddSingleton<IToolLocator, ToolLocator>();
        serviceCollection.AddSingleton<IReleaseClient, GitHubReleaseClient>();
        serviceCollection.AddSingleton<IToolVersionProbe>(services =>
            new CachingToolVersionProbe(new ToolVersionProbe(services.GetRequiredService<IProcessRunner>())));
        serviceCollection.AddSingleton<IToolInstaller, ToolInstaller>();
        serviceCollection.AddSingleton<IToolUpdateService, ToolUpdateService>();
        serviceCollection.AddSingleton<IFfmpegLocator, JellyfinFfmpegLocator>();
        serviceCollection.AddSingleton<IToolStatusService, ToolStatusService>();

        // yt-dlp
        serviceCollection.AddSingleton<IYtDlpCommandFactory, YtDlpCommandFactory>();

        // Detection: add a site's "video in a playlist" links by registering another inspector.
        serviceCollection.AddSingleton<IPlaylistLinkInspector, YouTubePlaylistLinkInspector>();
        serviceCollection.AddSingleton<IProbeResultParser, ProbeResultParser>();
        serviceCollection.AddSingleton<IMediaProber>(services =>
            new ThrottledMediaProber(ActivatorUtilities.CreateInstance<YtDlpMediaProber>(services), MaxConcurrentProbes));

        // Libraries
        serviceCollection.AddSingleton<ILibraryCatalog, JellyfinLibraryCatalog>();
        serviceCollection.AddSingleton<ILibraryNotifier, JellyfinLibraryNotifier>();

        // Downloads: add a group of yt-dlp options (subtitles, SponsorBlock, ...) by registering
        // another IDownloadOptionSet. They are applied in this order.
        serviceCollection.AddSingleton<IDownloadRequestValidator, DownloadRequestValidator>();
        serviceCollection.AddSingleton<IDownloadOptionSet, OutputOptions>();
        serviceCollection.AddSingleton<IDownloadOptionSet, ProgressOptions>();
        serviceCollection.AddSingleton<IDownloadOptionSet, ArchiveOptions>();
        serviceCollection.AddSingleton<IDownloadOptionSet, PlaylistOptions>();
        serviceCollection.AddSingleton<IDownloadOptionSet, VideoFormatOptions>();
        serviceCollection.AddSingleton<IDownloadOptionSet, AudioFormatOptions>();
        serviceCollection.AddSingleton<IDownloadArgumentsBuilder, DownloadArgumentsBuilder>();
        serviceCollection.AddSingleton<IYtDlpOutputParser, YtDlpOutputParser>();
        serviceCollection.AddSingleton<IDownloadErrorExplainer, DownloadErrorExplainer>();
        serviceCollection.AddSingleton<DownloadManager>();
        serviceCollection.AddSingleton<IDownloadManager>(services => services.GetRequiredService<DownloadManager>());
        serviceCollection.AddSingleton<IDownloadJobSource>(services => services.GetRequiredService<DownloadManager>());
        serviceCollection.AddSingleton<IDownloadRunner, DownloadRunner>();
        serviceCollection.AddHostedService<DownloadDispatcher>();
    }
}
