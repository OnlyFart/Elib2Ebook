using Core.Misc;
using Core.Types.Common;
using TypesBook = Core.Types.Book;

namespace Core.Tests.Types.Book;

public class BookTypeTests
{
    private static TypesBook.Book MakeBook()
        => new(new Uri("https://example.com/book"));

    private static async Task<TempFile> CreateTempFile(string dir, string name = "cover", string ext = ".jpg")
    {
        return await TempFile.Create(
            new Uri($"https://example.com/{name}{ext}"),
            dir,
            name,
            ext,
            new byte[]
            {
                1, 2, 3
            }
        );
    }

    [Fact]
    public void Book_CreateWithUrl_SetsUrl()
    {
        var url = new Uri("https://example.com/book");
        using var book = new TypesBook.Book(url);
        Assert.Equal(url, book.Url);
    }

    [Theory]
    [InlineData("ru")]
    [InlineData("en")]
    public void Book_DefaultLanguage_IsRu(string lang)
    {
        using var book = new TypesBook.Book(new Uri("https://example.com/book"))
        {
            Lang = lang
        };

        Assert.Equal(lang, book.Lang);
    }

    [Fact]
    public void Book_DefaultChapters_IsEmpty()
    {
        using var book = MakeBook();
        Assert.NotNull(book.Chapters);
        Assert.Empty(book.Chapters);
    }

    [Fact]
    public void Book_DefaultAdditionalFiles_IsEmpty()
    {
        using var book = MakeBook();
        Assert.NotNull(book.AdditionalFiles);
    }

    [Fact]
    public void Book_DefaultCoAuthors_IsEmpty()
    {
        using var book = MakeBook();
        Assert.NotNull(book.CoAuthors);
        Assert.Empty(book.CoAuthors);
    }

    [Fact]
    public async Task Book_Dispose_CoverIsNullAfterDispose()
    {
        var book = new TypesBook.Book(new Uri("https://example.com/book"));
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        book.Cover = await CreateTempFile(tempDir);
        book.Dispose();
        Assert.False(File.Exists(book.Cover.FilePath));
        Directory.Delete(tempDir);
    }

    [Fact]
    public void Author_Create_SetsProperties()
    {
        var author = new TypesBook.Author("Test Author", new Uri("https://example.com/author"));
        Assert.Equal("Test Author", author.Name);
        Assert.Equal("https://example.com/author", author.Url.ToString());
    }

    [Fact]
    public void Author_NullUrl_DoesNotThrow()
    {
        var author = new TypesBook.Author("Test Author", null);
        Assert.Equal("Test Author", author.Name);
        Assert.Null(author.Url);
    }

    [Theory]
    [InlineData("Test Seria", "1", "https://example.com/seria")]
    [InlineData("Empty Number", null, null)]
    public void Seria_Create_SetsProperties(string name, string number, string url)
    {
        var seria = new TypesBook.Seria
        {
            Name = name, Number = number, Url = url is null ? null : new Uri(url)
        };

        Assert.Equal(name, seria.Name);
        Assert.Equal(number, seria.Number);
        if (url is null)
        {
            Assert.Null(seria.Url);
        }
        else
        {
            Assert.NotNull(seria.Url);
        }
    }

    [Theory]
    [InlineData("Chapter 1", "Content here")]
    [InlineData("", "")]
    public void Chapter_Create_SetsProperties(string title, string content)
    {
        var chapter = new TypesBook.Chapter
        {
            Title = title, Content = content
        };

        Assert.Equal(title, chapter.Title);
        Assert.Equal(content, chapter.Content);
    }

    [Fact]
    public void AdditionalFileCollection_DefaultCollection_IsEmpty()
    {
        var collection = new TypesBook.AdditionalFileCollection();
        Assert.NotNull(collection);
        Assert.Empty(collection.Collection);
    }

    [Fact]
    public async Task AdditionalFileCollection_AddFile_StoresFile()
    {
        var collection = new TypesBook.AdditionalFileCollection();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var tempFile = await CreateTempFile(tempDir, "file", ".jpg");

        collection.Add(AdditionalTypeEnum.Images, tempFile);
        var files = collection.Get(AdditionalTypeEnum.Images);
        Assert.Single(files);
        Assert.Same(tempFile, files[0]);
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void AdditionalFileCollection_GetNonExistentType_ReturnsEmpty()
    {
        var collection = new TypesBook.AdditionalFileCollection();
        var files = collection.Get(AdditionalTypeEnum.Images);
        Assert.Empty(files);
    }
}
