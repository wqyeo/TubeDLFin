using System;
using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Configuration;
using Jellyfin.Plugin.TubeDLFin.Web;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.TubeDLFin;

/// <summary>
/// The plugin entry point that Jellyfin discovers. It only describes the plugin; the work is
/// done by the services registered in <see cref="PluginServiceRegistrator"/>.
/// </summary>
public class TubeDLFinPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TubeDLFinPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">The server's application paths.</param>
    /// <param name="xmlSerializer">The serializer used for the plugin configuration.</param>
    public TubeDLFinPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
    }

    /// <inheritdoc />
    public override string Name => PluginIdentity.Name;

    /// <inheritdoc />
    public override Guid Id => PluginIdentity.Id;

    /// <inheritdoc />
    public override string Description => "Download videos and music with yt-dlp straight into your Jellyfin libraries.";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages() => PluginWebPages.Create();
}
