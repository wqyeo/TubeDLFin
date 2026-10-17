using System;
using System.IO;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Libraries;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Downloads;

public class DownloadRequestValidatorTests
{
    private static readonly LibraryInfo[] Libraries = [DownloadPlans.Movies, DownloadPlans.Music];
    private readonly DownloadRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_BuildsPlan()
    {
        var request = Request(profile: Profile() with { Subfolder = " YouTube/Talks " }) with
        {
            NoPlaylist = true,
            FormatId = "137",
            PlaylistItems = "1-3, 7",
            Redownload = true,
            Title = "  Me at the zoo ",
        };

        var validation = _validator.Validate(request, Libraries);

        Assert.True(validation.IsValid, string.Join(" ", validation.Errors));
        var plan = validation.Plan!;
        Assert.Equal("https://www.youtube.com/watch?v=jNQXAC9IVRw", plan.Url.ToString());
        Assert.Same(DownloadPlans.Movies, plan.Library);
        Assert.Equal(Path.Combine("/media/movies", "YouTube", "Talks"), plan.Destination);
        Assert.Equal((true, "137", "1-3,7", true, "Me at the zoo"), (plan.NoPlaylist, plan.FormatId, plan.PlaylistItems, plan.Redownload, plan.Title));
    }

    [Fact]
    public void Validate_NoSubfolder_IsLibraryFolder()
    {
        var plan = _validator.Validate(Request(profile: Profile(library: DownloadPlans.Music, folder: "/mnt/nas/music")), Libraries).Plan;

        Assert.Equal("/mnt/nas/music", plan?.Destination);
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("YouTube/../../etc")]
    [InlineData("/etc")]
    [InlineData("a/./b")]
    [InlineData("a//b")]
    [InlineData("bad:name")]
    [InlineData("what?")]
    [InlineData("trailing.")]
    [InlineData(" padded /x")]
    public void Validate_UnsafeSubfolder_IsRefused(string subfolder)
    {
        var validation = _validator.Validate(Request(profile: Profile() with { Subfolder = subfolder }), Libraries);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, e => e.Contains("subfolder", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_FolderNotInLibrary_IsRefused()
    {
        var validation = _validator.Validate(Request(profile: Profile(folder: "/etc")), Libraries);

        Assert.Contains("Choose one of the folders of the library \"Movies\".", validation.Errors);
    }

    [Fact]
    public void Validate_UnknownLibrary_IsRefused()
    {
        var validation = _validator.Validate(Request(profile: Profile() with { LibraryId = "nope" }), Libraries);

        Assert.Contains("Choose a library.", validation.Errors);
    }

    [Theory]
    [InlineData("bv*+ba")]
    [InlineData("137 --exec rm")]
    [InlineData("-137")]
    [InlineData("137/best")]
    public void Validate_BadFormatId_IsRefused(string formatId)
    {
        Assert.Contains("The format id is not valid.", _validator.Validate(Request() with { FormatId = formatId }, Libraries).Errors);
    }

    [Theory]
    [InlineData("137")]
    [InlineData("251-drc")]
    [InlineData("hls_aac_160k")]
    [InlineData("dash-video=1080")]
    public void Validate_RealFormatIds_AreAccepted(string formatId)
    {
        Assert.True(_validator.Validate(Request() with { FormatId = formatId }, Libraries).IsValid);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1-10")]
    [InlineData("1,3,5")]
    [InlineData("20:")]
    [InlineData(":5")]
    [InlineData("1:10:2")]
    [InlineData("-5:")]
    [InlineData("1-3,7,10:")]
    public void Validate_PlaylistItems_Accepted(string items)
    {
        Assert.True(_validator.Validate(Request() with { PlaylistItems = items }, Libraries).IsValid);
    }

    [Theory]
    [InlineData("all")]
    [InlineData("1;2")]
    [InlineData("1,,2")]
    [InlineData("--exec")]
    public void Validate_BadPlaylistItems_AreRefused(string items)
    {
        Assert.False(_validator.Validate(Request() with { PlaylistItems = items }, Libraries).IsValid);
    }

    [Fact]
    public void Validate_HeightAndBitrateOutsideLists_AreRefused()
    {
        var validation = _validator.Validate(Request(profile: Profile() with { MaxHeight = 999, AudioQuality = 64 }), Libraries);

        Assert.Equal(2, validation.Errors.Count);
    }

    [Fact]
    public void Validate_UndefinedEnum_IsRefused()
    {
        var validation = _validator.Validate(Request(profile: Profile() with { Mode = (DownloadMode)42 }), Libraries);

        Assert.Contains("Unknown download option.", validation.Errors);
    }

    [Fact]
    public void Validate_EverythingMissing_ListsEveryProblem()
    {
        var validation = _validator.Validate(new DownloadRequest(), Libraries);

        Assert.Null(validation.Plan);
        Assert.Equal(["Enter a link.", "Choose the download options."], validation.Errors);
    }

    [Fact]
    public void Validate_LongTitle_IsShortened()
    {
        var plan = _validator.Validate(Request() with { Title = new string('x', 500) }, Libraries).Plan;

        Assert.Equal(300, plan?.Title?.Length);
    }

    private static DownloadRequest Request(DownloadProfile? profile = null) =>
        new() { Url = "https://www.youtube.com/watch?v=jNQXAC9IVRw", Profile = profile ?? Profile() };

    private static DownloadProfile Profile(LibraryInfo? library = null, string? folder = null)
    {
        library ??= DownloadPlans.Movies;
        return new DownloadProfile { LibraryId = library.Id, LibraryFolder = folder ?? library.Folders[0] };
    }
}
