using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Bookriver;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Elib2Ebook.Extensions;

namespace Elib2Ebook.Logic.Getters; 

public class BookriverGetter : GetterBase {
    public BookriverGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://bookriver.ru/");
    
    protected override string GetId(Uri url) {
        return url.Segments[2].Trim('/');
    }

    private async Task Authorize() {
        if (!_config.HasCredentials) {
            return;
        }

        var payload = new {
            email = _config.Login,
            password = _config.Password,
            rememberMe = 1
        };

        using var post = await _config.Client.PostAsJsonAsync($"https://api.bookriver.ru/api/v1/auth/login", payload);
        var data = await post.Content.ReadFromJsonAsync<BookRiverAuthResponse>();
        if (string.IsNullOrWhiteSpace(data.Token)) {
            throw new Exception("Не удалось авторизоваться");
        }
    }

    public override async Task<Book> Get(Uri url) {
        Init();

        await Authorize();
        var bookId = GetId(url);
        var uri = new Uri($"https://bookriver.ru/book/{bookId}");
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(uri);

        var book = new Book {
            Cover = await GetCover(doc, uri),
            Chapters = await FillChapters(uri, bookId),
            Title = doc.GetTextBySelector("h1[itemprop=name]"),
            Author = doc.GetTextBySelector("span[itemprop=author]"),
            Annotation = doc.QuerySelector("span[itemprop=description]")?.InnerHtml
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(Uri uri, string bookId) {
        var result = new List<Chapter>();
        var internalId = await GetInternalBookId(bookId);
        
        foreach (var bookChapter in await GetChapters(internalId)) {
            var chapter = new Chapter();
            Console.WriteLine($"Загружаем главу {bookChapter.Name.CoverQuotes()}");
            
            var doc = await GetChapter(bookChapter.Id);

            if (doc != default) {
                chapter.Title = bookChapter.Name;
                chapter.Images = await GetImages(doc, uri);
                chapter.Content = doc.DocumentNode.InnerHtml;

                result.Add(chapter);
            }
        }

        return result;
    }

    private async Task<string> GetInternalBookId(string bookId) {
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://bookriver.ru/reader/{bookId}"));
        return GetNextData<BookRiverBook>(doc, "book").Book.Id.ToString();
    }
    
    private static T GetNextData<T>(HtmlDocument doc, string node) {
        var json = doc.QuerySelector("#__NEXT_DATA__").InnerText;
        var bookProperty = JsonDocument.Parse(json)
            .RootElement
            .GetProperty("props")
            .GetProperty("pageProps")
            .GetProperty("state")
            .GetProperty(node)
            .GetRawText();
            
        return JsonSerializer.Deserialize<T>(bookProperty);
    }

    private async Task<HtmlDocument> GetChapter(long bookChapterId) {
        var response = await _config.Client.GetAsync(new Uri($"https://api.bookriver.ru/api/v1/books/chapter/text/{bookChapterId}"));
        if (response.StatusCode == HttpStatusCode.Forbidden) {
            return default;
        }
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BookRiverApiResponse<BookRiverChapterContent>>(content).Data.Content.AsHtmlDoc();
    }

    private async Task<BookRiverChapter[]> GetChapters(string bookId) {
        var response = await _config.Client.GetWithTriesAsync(new Uri($"https://api.bookriver.ru/api/v1/books/chapters/text/published?bookId={bookId}"));
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BookRiverApiResponse<BookRiverChapter[]>>(content).Data;
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("img[itemprop=contentUrl]")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, imagePath)) : Task.FromResult(default(Image));
    }
}