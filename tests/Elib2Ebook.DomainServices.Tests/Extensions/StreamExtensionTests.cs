using System.Text;
using Elib2Ebook.DomainServices.Extensions;

namespace Elib2Ebook.DomainServices.Tests.Extensions;

public class StreamExtensionTests
{
    private static MemoryStream MakeStream(string html)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(html));
    }

    [Theory]
    [InlineData("<p>test</p>")]
    [InlineData("")]
    public void AsHtmlDoc(string html)
    {
        Assert.NotNull(MakeStream(html).AsHtmlDoc());
    }

    [Theory]
    [InlineData("<p>тест</p>", "тест")]
    public void AsHtmlDoc_WithEncoding(string html, string expected)
    {
        Assert.Equal(expected, MakeStream(html).AsHtmlDoc(Encoding.UTF8).DocumentNode.InnerText.Trim());
    }

    [Fact]
    public void AsHtmlDoc_NullEncoding()
    {
        Assert.NotNull(MakeStream("<p>hello</p>").AsHtmlDoc());
    }
}
