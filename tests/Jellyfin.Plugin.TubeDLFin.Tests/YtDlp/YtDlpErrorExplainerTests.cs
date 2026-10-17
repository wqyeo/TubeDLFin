using Jellyfin.Plugin.TubeDLFin.YtDlp;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.YtDlp;

public class YtDlpErrorExplainerTests
{
    private readonly YtDlpErrorExplainer _explainer = new();

    [Theory]
    [InlineData("[Errno 30] Read-only file system: '/media/movies/x'", "read-only for Jellyfin. Check that the Docker volume has no \":ro\"")]
    [InlineData("[youtube] abc: Sign in to confirm you're not a bot.", "Set a cookies file")]
    [InlineData("unable to download video data: HTTP Error 403: Forbidden", "often temporary")]
    [InlineData("Unable to download webpage: HTTP Error 429: Too Many Requests", "rate-limiting")]
    public void Explain_KnownProblem_PutsAdviceBeforeMessage(string message, string advice)
    {
        var explained = _explainer.Explain(message);

        Assert.Contains(advice, explained, System.StringComparison.Ordinal);
        Assert.EndsWith($"({message})", explained, System.StringComparison.Ordinal);
    }

    [Fact]
    public void Explain_UnknownProblem_ReturnsMessage()
    {
        Assert.Equal("Video unavailable", _explainer.Explain("Video unavailable"));
    }
}
