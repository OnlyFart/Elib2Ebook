using Elib2Ebook.DomainServices.Extensions;
using HtmlAgilityPack;

namespace Elib2Ebook.DomainServices.Tests.Extensions;

public class HtmlDocumentExtensionsTests
{
    private static HtmlDocument Doc(string html)
    {
        var d = new HtmlDocument();
        d.LoadHtml(html);
        return d;
    }

    [Theory]
    [InlineData("<p>test</p>", "test")]
    [InlineData("<p>hello</p>", "hello")]
    public void AsString(string html, string expected)
    {
        Assert.Contains(expected, Doc(html).AsString());
    }

    [Theory]
    [InlineData("<p class=\"text\">hello</p>", ".text", "hello")]
    public void GetTextBySelector(string html, string selector, string expected)
    {
        Assert.Equal(expected, Doc(html).GetTextBySelector(selector));
    }

    [Theory]
    [InlineData("<p>hello <b>world</b></p>", "hello world")]
    public void GetText(string html, string expected)
    {
        Assert.Contains(expected, Doc(html).DocumentNode.SelectSingleNode("//p").GetText());
    }

    [Fact]
    public void GetText_NullNode()
    {
        Assert.Null(((HtmlNode)null).GetText());
    }

    [Theory]
    [InlineData("<div><p>keep</p></div><span>remove</span>", "span")]
    public void RemoveNodes_WithPredicate(string html, string tagToRemove)
    {
        var d = Doc(html);
        d.RemoveNodes(n => n.Name == tagToRemove);
        Assert.Null(d.DocumentNode.SelectSingleNode($"//{tagToRemove}"));
    }

    [Theory]
    [InlineData("<div><p class=\"remove\">a</p></div><p>b</p>", ".remove", "//p")]
    [InlineData("<div><span class=\"del\">x</span><span>y</span></div>", ".del", "//span")]
    public void RemoveNodes_WithSelector(string html, string selector, string remainingXpath)
    {
        var d = Doc(html);
        d.RemoveNodes(selector);
        Assert.Single(d.DocumentNode.SelectNodes(remainingXpath));
    }

    [Theory]
    [InlineData("<div><span>a</span><span>b</span></div>", "a", 1)]
    public void RemoveNodes_HtmlNodeWithPredicate(string html, string innerText, int expectedRemaining)
    {
        var d = Doc(html);
        d.DocumentNode.SelectSingleNode("//div").RemoveNodes(n => n.InnerText == innerText);
        Assert.Equal(expectedRemaining, d.DocumentNode.SelectNodes("//div/span")?.Count ?? 0);
    }

    [Theory]
    [InlineData("<div><span class=\"del\">x</span><span>y</span></div>", ".del", "//div/span")]
    public void RemoveNodes_HtmlNodeWithSelector(string html, string selector, string remainingXpath)
    {
        var d = Doc(html);
        d.DocumentNode.SelectSingleNode("//div").RemoveNodes(selector);
        Assert.Single(d.DocumentNode.SelectNodes(remainingXpath));
    }
}
