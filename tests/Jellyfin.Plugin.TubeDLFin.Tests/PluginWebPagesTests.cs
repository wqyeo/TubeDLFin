using System.IO;
using System.Linq;
using System.Reflection;
using Jellyfin.Plugin.TubeDLFin.Web;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests;

public class PluginWebPagesTests
{
    private static readonly Assembly PluginAssembly = typeof(TubeDLFinPlugin).Assembly;

    [Fact]
    public void Create_EveryPageResource_IsEmbedded()
    {
        var embedded = PluginAssembly.GetManifestResourceNames();

        Assert.All(PluginWebPages.Create(), page => Assert.Contains(page.EmbeddedResourcePath, embedded));
    }

    [Fact]
    public void Create_MainPage_IsInSidebarWithIcon()
    {
        var main = Assert.Single(PluginWebPages.Create(), page => page.EnableInMainMenu);

        Assert.Equal(PluginIdentity.Name, main.Name);
        Assert.Equal(PluginIdentity.Name, main.DisplayName);
        Assert.Equal(PluginWebPages.MenuIcon, main.MenuIcon);
    }

    [Fact]
    public void Create_ReturnsNewInstancesEachCall()
    {
        var first = PluginWebPages.Create();
        var second = PluginWebPages.Create();

        Assert.NotSame(first[0], second[0]);
    }

    [Fact]
    public void MainPage_DataController_PointsAtScriptPage()
    {
        var html = ReadResource(PluginWebPages.Create().Single(p => p.EnableInMainMenu).EmbeddedResourcePath);

        Assert.Contains($"data-controller=\"__plugin/{PluginWebPages.ScriptName}\"", html, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ScriptPage_IsEsModuleWithDefaultExport()
    {
        // jellyfin-web imports the script and calls `new module.default(view, params)`.
        var script = ReadResource(PluginWebPages.Create().Single(p => p.Name == PluginWebPages.ScriptName).EmbeddedResourcePath);

        Assert.Matches(@"export\s*\{[^}]*\bas default\b[^}]*\}", script);
    }

    private static string ReadResource(string name)
    {
        using var stream = PluginAssembly.GetManifestResourceStream(name);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
