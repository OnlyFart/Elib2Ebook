using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Bookstab;
using HtmlAgilityPack;

namespace Elib2Ebook.Logic.Getters; 

public class BookstabGetter : GetterBase {
    public BookstabGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://bookstab.ru/");

    protected override string GetId(Uri url) {
        return url.Segments[2].Trim('/');
    }

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        var uri = new Uri($"https://bookstab.ru/book/{bookId}");
        var response = await _config.Client.GetWithTriesAsync(new Uri($"https://api.bookstab.ru/api/reader-get/{bookId}"));
        var data = await response.Content.ReadFromJsonAsync<BookstabApiResponse>();

        var book = new Book {
            Cover = await GetCover(data),
            Chapters = await FillChapters(data, uri, bookId),
            Title = data.Book.Title,
            Author = data.Book.User.Pseudonym,
            Annotation = GetAnnotation(data.Book)
        };
            
        return book;
    }
    
    private static string GetAnnotation(BookstabBook book) {
        return string.IsNullOrWhiteSpace(book.Excerpt) ? 
            string.Empty : 
            string.Join("", book.Excerpt.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(s => $"<p>{s.Trim()}</p>"));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(BookstabApiResponse response, Uri uri, string bookId) {
        var result = new List<Chapter>();

        foreach (var bookChapter in response.Book.ChaptersShow) {
            var chapter = new Chapter();
            Console.WriteLine($"Загружаю главу {bookChapter.Title.CoverQuotes()}");
            
            var doc = await GetChapter(bookChapter.Id, bookId);

            if (doc != default) {
                chapter.Title = bookChapter.Title;
                chapter.Images = await GetImages(doc, uri);
                chapter.Content = doc.DocumentNode.InnerHtml;

                result.Add(chapter);
            }
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(int bookChapterId, string bookId) {
        var response = await _config.Client.GetFromJsonAsync<BookstabApiResponse>($"https://api.bookstab.ru/api/reader-get/{bookId}/{bookChapterId}");
        return string.IsNullOrWhiteSpace(response.Chapter.Body) ? default : response.Chapter.Body.AsHtmlDoc();
    }

    private Task<Image> GetCover(BookstabApiResponse response) {
        var imagePath = response.Book.Image;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri($"https://api.bookstab.ru/storage/{imagePath}")) : Task.FromResult(default(Image));
    }
}