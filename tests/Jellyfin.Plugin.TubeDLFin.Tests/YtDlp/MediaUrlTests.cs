using System;
using Jellyfin.Plugin.TubeDLFin.YtDlp;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.YtDlp;

public class MediaUrlTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=jNQXAC9IVRw")]
    [InlineData("  https://youtu.be/jNQXAC9IVRw?list=PL1  ")]
    [InlineData("http://example.com/video.mp4")]
    [InlineData("https://soundcloud.com/forss/flickermood")]
    public void TryCreate_HttpLinks_AreAccepted(string text)
    {
        Assert.True(MediaUrl.TryCreate(text, out var url, out var error));
        Assert.Null(error);
        Assert.Equal(text.Trim(), url.Value.OriginalString);
    }

    [Theory]
    [InlineData(null, "Enter a link.")]
    [InlineData("   ", "Enter a link.")]
    [InlineData("www.youtube.com/watch?v=x", "Enter a full link")]
    [InlineData("ftp://example.com/a", "Enter a full link")]
    [InlineData("file:///etc/passwd", "Enter a full link")]
    [InlineData("javascript:alert(1)", "Enter a full link")]
    [InlineData("-f bestvideo", "can't contain spaces")]
    [InlineData("https://example.com/a\nb", "can't contain spaces")]
    [InlineData("https://user:secret@example.com/a", "user name or password")]
    public void TryCreate_InvalidInput_ExplainsWhy(string? text, string expected)
    {
        Assert.False(MediaUrl.TryCreate(text, out var url, out var error));
        Assert.Null(url);
        Assert.Contains(expected, error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryCreate_TooLong_IsRefused()
    {
        var text = "https://example.com/" + new string('a', MediaUrl.MaxLength);

        Assert.False(MediaUrl.TryCreate(text, out _, out var error));
        Assert.Contains("2048", error, StringComparison.Ordinal);
    }
}
