using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.TubeDLFin.Web;

/// <summary>
/// The dashboard resources the plugin serves. The resources themselves are built in
/// <c>web/</c> and embedded by <c>Web.targets</c>.
/// </summary>
public static class PluginWebPages
{
    /// <summary>
    /// The page name of the main page, as used in <c>/web/configurationpage?name=...</c>.
    /// </summary>
    public const string MainPageName = PluginIdentity.Name;

    /// <summary>
    /// The page name of the page script. It must match the page's
    /// <c>data-controller="__plugin/..."</c> attribute in <c>configPage.html</c>.
    /// </summary>
    public const string ScriptName = "tubedlfin.js";

    /// <summary>
    /// The Material icon shown next to the sidebar entry.
    /// </summary>
    public const string MenuIcon = "download";

    private const string ResourcePrefix = "Jellyfin.Plugin.TubeDLFin.Web.";

    /// <summary>
    /// Creates the descriptions of every page the plugin serves. A new list is returned on
    /// each call because <see cref="PluginPageInfo"/> is mutable.
    /// </summary>
    /// <returns>The page descriptions.</returns>
    public static IReadOnlyList<PluginPageInfo> Create() =>
    [
        new PluginPageInfo
        {
            Name = MainPageName,
            DisplayName = PluginIdentity.Name,
            EmbeddedResourcePath = ResourcePrefix + "configPage.html",
            EnableInMainMenu = true,
            MenuIcon = MenuIcon,
        },
        new PluginPageInfo
        {
            Name = ScriptName,
            EmbeddedResourcePath = ResourcePrefix + ScriptName,
        },
    ];
}
