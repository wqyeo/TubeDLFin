using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Platform;

public class DriveInfoDiskSpaceProviderTests
{
    private readonly DriveInfoDiskSpaceProvider _provider = new();

    [Fact]
    public void GetFreeBytes_ExistingAndNotYetCreatedFolders()
    {
        using var temp = new TempDirectory();

        var existing = _provider.GetFreeBytes(temp.Path);
        var missing = _provider.GetFreeBytes(temp.Combine("not", "created", "yet"));

        Assert.True(existing > 0);
        Assert.True(missing > 0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetFreeBytes_NoPath_IsNull(string path)
    {
        Assert.Null(_provider.GetFreeBytes(path));
    }
}
