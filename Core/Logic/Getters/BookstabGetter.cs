using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Bookstab;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class BookstabGetter : GetterBase {
    public BookstabGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://bookstab.ru/");

    private Uri _apiUrl => new($"https://api.{SystemUrl.Host}/");

    protected override string GetId(Uri url) => url.GetSegment(2);

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = SystemUrl.MakeRelativeUri($"/book/{bookId}");
        using var response = await Config.Client.GetWithTriesAsync(_apiUrl.MakeRelativeUri($"/api/reader-get/{bookId}"));
        var data = await response.Content.ReadFromJsonAsync<BookstabApiResponse>();

        var book = new Book(url) {
            Cover = await GetCover(data),
            Chapters = await FillChapters(data, url, bookId),
            Title = data?.Book.Title,
            Author = GetAuthor(data),
            Annotation = GetAnnotation(data?.Book)
        };
            
        return book;
    }

    private Author GetAuthor(BookstabApiResponse book) {
        return new Author(book.Book.User.Pseudonym, SystemUrl.MakeRelativeUri($"/user/{book.Book.User.Name}"));
    }
    
    private static string GetAnnotation(BookstabBook book) {
        return string.IsNullOrWhiteSpace(book.Excerpt) ? 
            string.Empty : 
            string.Join("", book.Excerpt.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().CoverTag("p")));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(BookstabApiResponse response, Uri uri, string bookId) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        foreach (var bookChapter in SliceToc(response.Book.ChaptersShow, c => c.Title)) {
            var chapter = new Chapter {
                Title = bookChapter.Title
            };

            Config.Logger.LogInformation($"Загружаю главу {bookChapter.Title.CoverQuotes()}");
            
            var doc = await GetChapter(bookChapter.Id, bookId);

            if (doc != default) {
                chapter.Images = await GetImages(doc, uri);
                chapter.Content = doc.DocumentNode.InnerHtml;
            }
            
            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(int chapterId, string bookId) {
        while (true) {
            using var response = await Config.Client.GetAsync(_apiUrl.MakeRelativeUri($"/api/reader-get/{bookId}/{chapterId}"));
            if (response.StatusCode == HttpStatusCode.TooManyRequests) {
                Config.Logger.LogInformation("Очень много запросов. Подождем...");
                await Task.Delay(TimeSpan.FromSeconds(10));
                continue;
            }

            var data = await response.Content.ReadFromJsonAsync<BookstabApiResponse>();
            return string.IsNullOrWhiteSpace(data?.Chapter.Body) ? default : data.Chapter.Body.AsHtmlDoc();
        }
    }

    private Task<Image> GetCover(BookstabApiResponse response) {
        var imagePath = response.Book.Image;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(_apiUrl.MakeRelativeUri($"/storage/{imagePath}")) : Task.FromResult(default(Image));
    }
}