using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Bookriver;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class BookriverGetter : GetterBase {
    public BookriverGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://bookriver.ru/");

    private string _token;
    
    protected override string GetId(Uri url) {
        return url.Segments[2].Trim('/');
    }

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }

        var payload = new {
            email = Config.Options.Login,
            password = Config.Options.Password,
            rememberMe = 1
        };

        using var post = await Config.Client.PostAsJsonAsync("https://api.bookriver.ru/api/v1/auth/login", payload);
        var data = await post.Content.ReadFromJsonAsync<BookRiverAuthResponse>();
        if (!string.IsNullOrWhiteSpace(data.Token)) {
            Console.WriteLine("Успешно авторизовались");
            _token = data.Token;
        } else {
            throw new Exception("Не удалось авторизоваться");
        }
    }

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = new Uri($"https://bookriver.ru/book/{bookId}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(url, bookId),
            Title = doc.GetTextBySelector("h1[itemprop=name]"),
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("span[itemprop=description]")?.InnerHtml,
            Seria = GetSeria(doc, url)
        };
            
        return book;
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("span[itemprop=author] a");
        return new Author(a.GetText(), new Uri(url, a.Attributes["href"].Value));
    }

    private static Seria GetSeria(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("a[href^=/series/]");
        if (a == default) {
            return default;
        }

        var span = a.QuerySelector("+ span");
        if (span == default) {
            return default;
        }

        return new Seria {
            Name = a.GetText()[5..].Trim(),
            Number = span.GetText().Trim().Trim('#'),
            Url = new Uri(url, a.Attributes["href"].Value)
        };
    }

    private async Task<IEnumerable<Chapter>> FillChapters(Uri uri, string bookId) {
        var result = new List<Chapter>();
        var internalId = await GetInternalBookId(bookId);
        
        foreach (var bookChapter in await GetToc(internalId)) {
            Console.WriteLine($"Загружаю главу {bookChapter.Name.CoverQuotes()}");
            var chapter = new Chapter {
                Title = bookChapter.Name
            };
            
            var doc = await GetChapter(bookChapter.Id);
            if (doc != default) {
                chapter.Images = await GetImages(doc, uri);
                chapter.Content = doc.DocumentNode.InnerHtml;
            }
            
            result.Add(chapter);
        }

        return result;
    }
    
    protected virtual HttpRequestMessage GetMessage(Uri uri) {
        var message = new HttpRequestMessage(HttpMethod.Get, uri);
        message.Version = Config.Client.DefaultRequestVersion;
        if (!string.IsNullOrWhiteSpace(_token)) {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        foreach (var header in Config.Client.DefaultRequestHeaders) {
            message.Headers.Add(header.Key, header.Value);
        }
        
        return message;
    }

    private async Task<string> GetInternalBookId(string bookId) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://bookriver.ru/reader/{bookId}"));
        return GetNextData<BookRiverBook>(doc, "book").Book.Id.ToString();
    }

    private static T GetNextData<T>(HtmlDocument doc, string node) {
        var json = doc.QuerySelector("#__NEXT_DATA__").InnerText;

        return JsonDocument.Parse(json)
            .RootElement
            .GetProperty("props")
            .GetProperty("pageProps")
            .GetProperty("state")
            .GetProperty(node)
            .GetRawText()
            .Deserialize<T>();
    }

    private async Task<HtmlDocument> GetChapter(long bookChapterId) {
        var response = await Config.Client.SendAsync(GetMessage(new Uri($"https://api.bookriver.ru/api/v1/books/chapter/text/{bookChapterId}")));
        if (response.StatusCode == HttpStatusCode.Forbidden) {
            return default;
        }
        
        var content = await response.Content.ReadAsStringAsync();
        return content.Deserialize<BookRiverApiResponse<BookRiverChapterContent>>().Data.Content.AsHtmlDoc();
    }

    private async Task<BookRiverChapter[]> GetToc(string bookId) {
        var response = await Config.Client.GetWithTriesAsync(new Uri($"https://api.bookriver.ru/api/v1/books/chapters/text/published?bookId={bookId}"));
        var content = await response.Content.ReadAsStringAsync();
        return content.Deserialize<BookRiverApiResponse<BookRiverChapter[]>>().Data;
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("img[itemprop=contentUrl]")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, imagePath)) : Task.FromResult(default(Image));
    }
}