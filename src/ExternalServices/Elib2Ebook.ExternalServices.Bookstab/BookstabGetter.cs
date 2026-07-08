using System.Net;
using System.Net.Http.Json;
using Elib2Ebook.Domain.Book;
using Elib2Ebook.Domain.Common;
using Elib2Ebook.DomainServices.Configs;
using Elib2Ebook.DomainServices.Extensions;
using Elib2Ebook.DomainServices.Getters;
using Elib2Ebook.ExternalServices.Bookstab.Types;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Elib2Ebook.ExternalServices.Bookstab;

public class BookstabGetter(BookGetterConfig config) : GetterBase(config)
{
    protected override Uri SystemUrl => new("https://bookstab.ru/");

    private Uri _apiUrl => new($"https://api.{SystemUrl.Host}/");

    protected override string GetId(Uri url)
        => url.GetSegment(2);

    public override async Task<Book> Get(Uri url)
    {
        var bookId = GetId(url);
        url = SystemUrl.MakeRelativeUri($"/book/{bookId}");
        using var response = await Config.Client.GetWithTriesAsync(_apiUrl.MakeRelativeUri($"/api/reader-get/{bookId}"));
        var data = await response.Content.ReadFromJsonAsync<ApiResponse>();

        var book = new Book(url)
        {
            Cover = await GetCover(data),
            Chapters = await FillChapters(data, url, bookId),
            Title = data?.Book.Title,
            Author = GetAuthor(data),
            Annotation = GetAnnotation(data?.Book)
        };

        return book;
    }

    private Author GetAuthor(ApiResponse book)
    {
        return new Author(book.Book.User.Pseudonym, SystemUrl.MakeRelativeUri($"/user/{book.Book.User.Name}"));
    }

    private static string GetAnnotation(BookstabBook book)
    {
        return string.IsNullOrWhiteSpace(book.Excerpt) ?
            string.Empty :
            string.Join("", book.Excerpt.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().CoverTag("p")));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(ApiResponse response, Uri uri, string bookId)
    {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters)
        {
            return result;
        }

        foreach (var bookChapter in SliceToc(response.Book.ChaptersShow, c => c.Title))
        {
            var chapter = new Chapter
            {
                Title = bookChapter.Title
            };

            Config.Logger.LogInformation($"Загружаю главу {bookChapter.Title.CoverQuotes()}");

            var doc = await GetChapter(bookChapter.Id, bookId);

            if (doc != null)
            {
                chapter.Images = await GetImages(doc, uri);
                chapter.Content = doc.DocumentNode.InnerHtml;
            }

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(int chapterId, string bookId)
    {
        while (true)
        {
            using var response = await Config.Client.GetAsync(_apiUrl.MakeRelativeUri($"/api/reader-get/{bookId}/{chapterId}"));
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                Config.Logger.LogInformation("Очень много запросов. Подождем...");
                await Task.Delay(TimeSpan.FromSeconds(10));
                continue;
            }

            var data = await response.Content.ReadFromJsonAsync<ApiResponse>();
            return string.IsNullOrWhiteSpace(data?.Chapter.Body) ? null : data.Chapter.Body.AsHtmlDoc();
        }
    }

    private Task<TempFile> GetCover(ApiResponse response)
    {
        var imagePath = response.Book.Image;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(_apiUrl.MakeRelativeUri($"/storage/{imagePath}")) : Task.FromResult(default(TempFile));
    }
}
