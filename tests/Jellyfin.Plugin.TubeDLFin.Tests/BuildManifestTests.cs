using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests;

/// <summary>
/// build.yaml (read by JPRM) and Directory.Build.props duplicate facts from the code;
/// these tests stop them from drifting apart.
/// </summary>
public class BuildManifestTests
{
    private static readonly string BuildYaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "build.yaml"));

    [Fact]
    public void Guid_MatchesPluginIdentity()
    {
        Assert.Equal(PluginIdentity.Id, Guid.Parse(YamlValue("guid")));
    }

    [Fact]
    public void Name_MatchesPluginIdentity()
    {
        Assert.Equal(PluginIdentity.Name, YamlValue("name"));
    }

    [Fact]
    public void Version_MatchesDirectoryBuildProps()
    {
        var props = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Directory.Build.props.xml"));
        var propsVersion = props.Root!.Element("PropertyGroup")!.Element("Version")!.Value;

        Assert.Equal(propsVersion, YamlValue("version"));
    }

    [Fact]
    public void Artifacts_ContainPluginAssembly()
    {
        Assert.Contains($"\"{typeof(TubeDLFinPlugin).Assembly.GetName().Name}.dll\"", BuildYaml, StringComparison.Ordinal);
    }

    private static string YamlValue(string key)
    {
        var match = Regex.Match(BuildYaml, $"^{key}:\\s*\"(?<value>[^\"]*)\"", RegexOptions.Multiline);
        Assert.True(match.Success, $"build.yaml has no quoted '{key}' value");
        return match.Groups["value"].Value;
    }
}
