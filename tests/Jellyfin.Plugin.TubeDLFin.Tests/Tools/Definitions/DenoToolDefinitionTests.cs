using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Tools.Definitions;

public class DenoToolDefinitionTests
{
    private readonly DenoToolDefinition _definition = new();

    public static TheoryData<ToolPlatform, string, string> SupportedPlatforms => new()
    {
        { Platforms.LinuxX64, "deno-x86_64-unknown-linux-gnu.zip", "deno" },
        { Platforms.LinuxArm64, "deno-aarch64-unknown-linux-gnu.zip", "deno" },
        { Platforms.WindowsX64, "deno-x86_64-pc-windows-msvc.zip", "deno.exe" },
        { Platforms.WindowsArm64, "deno-aarch64-pc-windows-msvc.zip", "deno.exe" },
        { Platforms.MacX64, "deno-x86_64-apple-darwin.zip", "deno" },
        { Platforms.MacArm64, "deno-aarch64-apple-darwin.zip", "deno" },
    };

    public static IEnumerable<TheoryDataRow<ToolPlatform>> UnsupportedPlatforms =>
    [
        new(Platforms.LinuxMuslX64),
        new(Platforms.LinuxMuslArm64),
        new(Platforms.LinuxArmV7),
        new(Platforms.WindowsX86),
        new(Platforms.FreeBsdX64),
    ];

    [Theory]
    [MemberData(nameof(SupportedPlatforms))]
    public void GetAsset_SupportedPlatform_IsZipWithOwnChecksum(ToolPlatform platform, string expectedZip, string expectedEntry)
    {
        var asset = _definition.GetAsset(platform);

        Assert.Equal(new ToolAsset(expectedZip, expectedZip + ".sha256sum", expectedEntry), asset);
    }

    [Theory]
    [MemberData(nameof(UnsupportedPlatforms))]
    public void GetAsset_UnsupportedPlatform_IsNull(ToolPlatform platform)
    {
        Assert.Null(_definition.GetAsset(platform));
    }

    [Fact]
    public void ParseVersion_ReadsDenoLine()
    {
        const string Output = "deno 2.9.7 (stable, release, aarch64-unknown-linux-gnu)\nv8 14.2.1\ntypescript 5.9.2\n";

        Assert.Equal("2.9.7", _definition.ParseVersion(Output));
    }

    [Theory]
    [InlineData("")]
    [InlineData("node v24.0.0")]
    public void ParseVersion_UnexpectedOutput_IsNull(string output)
    {
        Assert.Null(_definition.ParseVersion(output));
    }

    [Fact]
    public void VersionFromTag_DropsLeadingV()
    {
        Assert.Equal("2.9.7", _definition.VersionFromTag("v2.9.7"));
    }
}
