using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Tools.Definitions;

public class YtDlpToolDefinitionTests
{
    private readonly YtDlpToolDefinition _definition = new();

    public static TheoryData<ToolPlatform, string> SupportedPlatforms => new()
    {
        { Platforms.LinuxX64, "yt-dlp_linux" },
        { Platforms.LinuxArm64, "yt-dlp_linux_aarch64" },
        { Platforms.LinuxMuslX64, "yt-dlp_musllinux" },
        { Platforms.LinuxMuslArm64, "yt-dlp_musllinux_aarch64" },
        { Platforms.WindowsX64, "yt-dlp.exe" },
        { Platforms.WindowsArm64, "yt-dlp_arm64.exe" },
        { Platforms.WindowsX86, "yt-dlp_x86.exe" },
        { Platforms.MacX64, "yt-dlp_macos" },
        { Platforms.MacArm64, "yt-dlp_macos" },
    };

    public static IEnumerable<TheoryDataRow<ToolPlatform>> UnsupportedPlatforms =>
    [
        new(Platforms.LinuxArmV7),
        new(Platforms.FreeBsdX64),
    ];

    [Theory]
    [MemberData(nameof(SupportedPlatforms))]
    public void GetAsset_SupportedPlatform_IsRawBinaryCheckedAgainstSha256Sums(ToolPlatform platform, string expectedFile)
    {
        var asset = _definition.GetAsset(platform);

        Assert.Equal(new ToolAsset(expectedFile, "SHA2-256SUMS"), asset);
    }

    [Theory]
    [MemberData(nameof(UnsupportedPlatforms))]
    public void GetAsset_UnsupportedPlatform_IsNull(ToolPlatform platform)
    {
        Assert.Null(_definition.GetAsset(platform));
    }

    [Fact]
    public void ParseVersion_ReadsFirstLine()
    {
        Assert.Equal("2026.08.19", _definition.ParseVersion("2026.08.19\n"));
    }

    [Fact]
    public void ParseVersion_EmptyOutput_IsNull()
    {
        Assert.Null(_definition.ParseVersion("  \n"));
    }

    [Fact]
    public void VersionFromTag_IsTheTag()
    {
        Assert.Equal("2026.08.19", _definition.VersionFromTag("2026.08.19"));
    }

    [Fact]
    public void GetExecutableName_AddsExeOnWindowsOnly()
    {
        Assert.Equal("yt-dlp", _definition.GetExecutableName(Platforms.LinuxArm64));
        Assert.Equal("yt-dlp.exe", _definition.GetExecutableName(Platforms.WindowsX64));
    }
}
