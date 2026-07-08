using System.Text;
using Elib2Ebook.DomainServices.Extensions;

namespace Elib2Ebook.DomainServices.Tests.Extensions;

public class StringBuilderExtensionsTests
{
    [Theory]
    [InlineData("<p>test</p>", "test")]
    [InlineData("", "")]
    public void AsHtmlDoc(string html, string expectedText)
    {
        Assert.Equal(expectedText, new StringBuilder(html).AsHtmlDoc().DocumentNode.InnerText.Trim());
    }
}
