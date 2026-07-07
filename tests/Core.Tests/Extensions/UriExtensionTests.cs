using Core.Extensions;

namespace Core.Tests.Extensions;

public class UriExtensionTests
{
    private static readonly Uri Base = new("https://example.com/path/to/file.txt?q=1&r=2");

    [Fact]
    public void GetFileName()
    {
        Assert.Equal("file.txt", Base.GetFileName());
    }

    [Theory]
    [InlineData("https://example.com/path/to/file.txt?q=1&r=2/new", "new")]
    [InlineData("https://example.com/path/to/file.txt?q=1&r=2/new/", "/new/")]
    public void AppendSegment(string expected, string segment)
    {
        Assert.Equal(expected, Base.AppendSegment(segment).ToString());
    }

    [Fact]
    public void GetSegment()
    {
        Assert.Equal("b", new Uri("https://example.com/a/b/c").GetSegment(2));
    }

    [Theory]
    [InlineData("r=2")]
    [InlineData("q=1")]
    public void AppendQueryParameter(string expectedParam)
    {
        Assert.Contains(expectedParam, new Uri("https://example.com/page?q=1").AppendQueryParameter("r", 2).Query);
    }

    [Fact]
    public void GetQueryParameter()
    {
        Assert.Equal("value", new Uri("https://example.com/page?name=value").GetQueryParameter("name"));
    }

    [Fact]
    public void ReplaceHost()
    {
        Assert.Equal("https://new.com/path", new Uri("https://old.com/path").ReplaceHost("new.com").ToString());
    }

    [Theory]
    [InlineData("https://example.com/p1", "https://example.com/p2", true)]
    [InlineData("https://www.example.com/p", "https://example.com/p", true)]
    [InlineData("https://example.com/p", "https://other.com/p", false)]
    public void IsSameHost(string url1, string url2, bool expected)
    {
        Assert.Equal(expected, new Uri(url1).IsSameHost(new Uri(url2)));
    }

    [Theory]
    [InlineData("https://sub.example.com/p", "https://example.com/p", true)]
    [InlineData("https://sub.other.com/p", "https://example.com/p", false)]
    [InlineData("https://example.com/p", "https://example.com/p", true)]
    public void IsSameSubDomain(string url1, string url2, bool expected)
    {
        Assert.Equal(expected, new Uri(url1).IsSameSubDomain(new Uri(url2)));
    }

    [Fact]
    public void MakeRelativeUri()
    {
        Assert.Equal("https://example.com/base/relative/path", new Uri("https://example.com/base/").MakeRelativeUri("relative/path").ToString());
    }
}
