using Elib2Ebook.Domain.Book;
using Elib2Ebook.DomainServices.Builders;
using Elib2Ebook.DomainServices.Configs;
using Elib2Ebook.DomainServices.Misc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elib2Ebook.DomainServices.Tests.Misc;

public class BuilderProviderTests
{
    [Theory]
    [InlineData("fb2", typeof(Fb2Builder))]
    [InlineData("epub", typeof(EpubBuilder))]
    [InlineData("json", typeof(JsonBuilder))]
    [InlineData("cbz", typeof(CbzBuilder))]
    [InlineData("txt", typeof(TxtBuilder))]
    [InlineData("JSON_LITE", typeof(JsonLiteBuilder))]
    public void Get(string format, Type type)
    {
        Assert.IsType(
            type,
            BuilderProvider.Get(
                format,
                new Options(
                    new[]
                    {
                        "https://x.com"
                    }),
                NullLogger.Instance));
    }

    [Fact]
    public void Get_Invalid()
    {
        Assert.Throws<ArgumentException>(() => BuilderProvider.Get(
            "bad",
            new Options(
                new[]
                {
                    "https://x.com"
                }),
            NullLogger.Instance));
    }
}

public class BookNameBuilderLogicTests
{
    private static Book Book(string title, string author, Seria seria = null)
    {
        return new Book(new Uri("https://x.com"))
        {
            Title = title, Author = new Author(author), Seria = seria
        };
    }

    [Theory]
    [InlineData("{Book.Title}", "Test", "Test")]
    [InlineData("{Book.Title} - {Author.Name}", "My Book", "My Book - Author")]
    [InlineData("{Book.Title} - {Seria.Name} #{Seria.Number}", "Book", "Book - S #1")]
    [InlineData("{Book.Title} by {Author.Name}", "Book", "Book by Author")]
    public void Build(string pattern, string title, string expected)
    {
        var seria = pattern.Contains("Seria") ? new Seria
        {
            Name = "S", Number = "1"
        } : null;

        Assert.Equal(expected, BookNameBuilder.Build(pattern, Book(title, "Author", seria)));
    }

    [Fact]
    public void Build_WithQuotes()
    {
        Assert.DoesNotContain("\"", BookNameBuilder.Build("\"{Book.Title}\"", Book("Test", "A")));
    }
}
