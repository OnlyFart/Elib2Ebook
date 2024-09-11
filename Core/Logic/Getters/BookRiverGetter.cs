using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Bookriver;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class BookriverGetter : GetterBase {
    public BookriverGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://bookriver.ru/");

    private Uri _apiUrl => new($"https://api.{SystemUrl.Host}/");

    private string _token;
    
    protected override string GetId(Uri url) => url.GetSegment(2);

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }

        var payload = new {
            email = Config.Options.Login,
            password = Config.Options.Password,
            rememberMe = 1
        };

        using var post = await Config.Client.PostAsJsonAsync(_apiUrl.MakeRelativeUri("/api/v1/auth/login"), payload);
        var data = await post.Content.ReadFromJsonAsync<BookRiverAuthResponse>();
        if (!string.IsNullOrWhiteSpace(data.Token)) {
            Config.Logger.LogInformation("Успешно авторизовались");
            _token = data.Token;
            Config.CookieContainer.Add(new Cookie("authToken", _token, "/", SystemUrl.Host));
        } else {
            throw new Exception("Не удалось авторизоваться");
        }
    }

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = SystemUrl.MakeRelativeUri($"/book/{bookId}");
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
        return new Author(a.GetText(), url.MakeRelativeUri(a.Attributes["href"].Value));
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
            Url = url.MakeRelativeUri(a.Attributes["href"].Value)
        };
    }

    private async Task<IEnumerable<Chapter>> FillChapters(Uri uri, string bookId) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }
        
        var internalId = await GetInternalBookId(bookId);
        
        foreach (var bookChapter in await GetToc(internalId)) {
            Config.Logger.LogInformation($"Загружаю главу {bookChapter.Name.CoverQuotes()}");
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
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri($"/reader/{bookId}"));
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
        var response = await Config.Client.SendAsync(GetMessage(_apiUrl.MakeRelativeUri($"/api/v1/books/chapter/text/{bookChapterId}")));
        if (response.StatusCode == HttpStatusCode.Forbidden) {
            return default;
        }
        
        var content = await response.Content.ReadAsStringAsync();
        return content.Deserialize<BookRiverApiResponse<BookRiverChapterContent>>().Data.Content.AsHtmlDoc();
    }

    private async Task<IEnumerable<BookRiverChapter>> GetToc(string bookId) {
        var response = await Config.Client.GetWithTriesAsync(_apiUrl.MakeRelativeUri($"/api/v1/books/chapters/text/published?bookId={bookId}"));
        var content = await response.Content.ReadAsStringAsync();
        return SliceToc(content.Deserialize<BookRiverApiResponse<BookRiverChapter[]>>().Data, c => c.Name);
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("img[itemprop=contentUrl]")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}