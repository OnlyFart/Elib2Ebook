using Core.Extensions;

namespace Core.Tests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("Hello World.txt", "Hello World.txt")]
    public void RemoveInvalidChars(string input, string expected)
    {
        Assert.Equal(expected, input.RemoveInvalidChars());
    }

    [Theory]
    [InlineData("test", "\"test\"")]
    public void CoverQuotes(string input, string expected)
    {
        Assert.Equal(expected, input.CoverQuotes());
    }

    [Theory]
    [InlineData("hello", 10, "hello")]
    [InlineData("hello world", 5, "hello")]
    public void Crop(string input, int len, string expected)
    {
        Assert.Equal(expected, input.Crop(len));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", "   ")]
    [InlineData("text", "text")]
    public void HtmlDecode(string input, string expected)
    {
        Assert.Equal(expected, input.HtmlDecode());
    }

    [Theory]
    [InlineData("text", "b", "<b>text</b>")]
    [InlineData("text", "", "text")]
    public void CoverTag(string input, string tag, string expected)
    {
        Assert.Equal(expected, input.CoverTag(tag));
    }

    [Theory]
    [InlineData("line1\nline2\tline3")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ReplaceNewLine(string input)
    {
        var result = input.ReplaceNewLine();
        if (input is null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.DoesNotContain("\n", result);
            Assert.DoesNotContain("\t", result);
        }
    }

    [Theory]
    [InlineData("a   b  c", "a b c")]
    [InlineData("", "")]
    public void CollapseWhitespace(string input, string expected)
    {
        Assert.Equal(expected, input.CollapseWhitespace());
    }

    [Fact]
    public void HtmlEncode()
    {
        var result = "a<b>c".HtmlEncode();
        Assert.Contains("&", result);
    }

    [Fact]
    public void AsUri()
    {
        Assert.Equal("https://example.com/", "https://example.com".AsUri().ToString());
    }

    [Fact]
    public void CleanInvalidXmlChars()
    {
        Assert.Equal("test", "te\x00st".CleanInvalidXmlChars());
    }

    [Theory]
    [InlineData("<html><body><p>test</p></body></html>")]
    public void AsHtmlDoc(string html)
    {
        Assert.NotNull(html.AsHtmlDoc().DocumentNode);
    }

    [Theory]
    [InlineData("<html><body><p>test</p></body></html>")]
    public void AsXHtmlDoc(string html)
    {
        Assert.NotNull(html.AsXHtmlDoc());
    }
}
