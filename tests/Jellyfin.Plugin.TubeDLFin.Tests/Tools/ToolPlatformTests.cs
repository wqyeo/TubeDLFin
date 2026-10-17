using System.Runtime.InteropServices;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Tools;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Tools;

public class ToolPlatformTests
{
    [Theory]
    [InlineData(false, Architecture.Arm64, LibcFlavor.Glibc, "linux-arm64-glibc")]
    [InlineData(true, Architecture.X64, LibcFlavor.Musl, "linux-x64-musl")]
    public void FromEnvironment_Linux_DetectsLibc(bool musl, Architecture architecture, LibcFlavor expectedLibc, string expectedId)
    {
        var environment = Substitute.For<IRuntimeEnvironment>();
        environment.IsLinux.Returns(true);
        environment.IsMusl.Returns(musl);
        environment.ProcessArchitecture.Returns(architecture);

        var platform = ToolPlatform.FromEnvironment(environment);

        Assert.Equal(new ToolPlatform(OsFamily.Linux, architecture, expectedLibc), platform);
        Assert.Equal(expectedId, platform.Id);
    }

    [Fact]
    public void FromEnvironment_Windows_HasNoLibcAndExeSuffix()
    {
        var environment = Substitute.For<IRuntimeEnvironment>();
        environment.IsWindows.Returns(true);
        environment.ProcessArchitecture.Returns(Architecture.X64);

        var platform = ToolPlatform.FromEnvironment(environment);

        Assert.Equal("windows-x64", platform.Id);
        Assert.Equal("deno.exe", platform.ExecutableName("deno"));
    }

    [Fact]
    public void FromEnvironment_UnknownOs_IsOther()
    {
        var environment = Substitute.For<IRuntimeEnvironment>();
        environment.ProcessArchitecture.Returns(Architecture.X64);

        Assert.Equal(OsFamily.Other, ToolPlatform.FromEnvironment(environment).Os);
    }
}
