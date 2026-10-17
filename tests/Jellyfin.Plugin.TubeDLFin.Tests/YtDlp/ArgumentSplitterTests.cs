using System;
using Jellyfin.Plugin.TubeDLFin.YtDlp;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.YtDlp;

public class ArgumentSplitterTests
{
    [Theory]
    [InlineData(null, new string[0])]
    [InlineData("   ", new string[0])]
    [InlineData("--limit-rate 5M", new[] { "--limit-rate", "5M" })]
    [InlineData("  -N   4  ", new[] { "-N", "4" })]
    [InlineData("--proxy \"socks5://host:1080\"", new[] { "--proxy", "socks5://host:1080" })]
    [InlineData("--user-agent 'My Agent 1.0'", new[] { "--user-agent", "My Agent 1.0" })]
    [InlineData("--add-header \"Referer:https://a.example/\" --x", new[] { "--add-header", "Referer:https://a.example/", "--x" })]
    [InlineData("a\\ b c", new[] { "a b", "c" })]
    [InlineData("\"say \\\"hi\\\"\"", new[] { "say \"hi\"" })]
    [InlineData("'single \\ kept'", new[] { "single \\ kept" })]
    [InlineData("\"\" x", new[] { "", "x" })]
    [InlineData("pre\"quoted part\"post", new[] { "prequoted partpost" })]
    [InlineData("$(whoami) ;rm", new[] { "$(whoami)", ";rm" })]
    public void Split(string? text, string[] expected)
    {
        Assert.Equal(expected, ArgumentSplitter.Split(text));
    }

    [Theory]
    [InlineData("--proxy \"socks5://host")]
    [InlineData("it's")]
    public void Split_UnclosedQuote_Throws(string text)
    {
        Assert.Throws<FormatException>(() => ArgumentSplitter.Split(text));
    }
}
