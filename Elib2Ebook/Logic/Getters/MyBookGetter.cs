using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.MyBook;
using EpubSharp;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using OAuth;

namespace Elib2Ebook.Logic.Getters; 

public class MyBookGetter : GetterBase {
    public MyBookGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://mybook.ru/");

    private Uri AuthUrl => SystemUrl.MakeRelativeUri("/api/auth/");

    private const string CONSUMER_KEY = "830968793b2a44c688400319f4a77231";
    private const string CONSUMER_SECRET = "HhaxXsMryrLz49R4";
    private const string IDENTIFIER = "47b17a4c8231a28f06c6c836d0b5f6f2134cb74d";

    private HttpClient _apiClient;
    private MyBookAuth _token;

    private Uri GetMainUrl(Uri uri) {
        return SystemUrl.MakeRelativeUri($"{uri.GetSegment(1)}/{uri.GetSegment(2)}/{uri.GetSegment(3)}");
    }

    public override async Task Init() {
        _apiClient = new HttpClient();
        _apiClient.DefaultRequestHeaders.Add("User-Agent", "MyBook/6.6.0 (iPhone; iOS 16.0.3; Scale/3.00)");
        _apiClient.DefaultRequestHeaders.Add("Accept", "application/json; version=4");
        
        using var response = await _apiClient.PostAsJsonAsync(AuthUrl, new { identifier = IDENTIFIER });
        _token = await response.Content.ReadFromJsonAsync<MyBookAuth>();
    }

    private void SetAuthHeader(string method, Uri uri) {
        var client = new OAuthRequest {
            Method = method,
            SignatureMethod = OAuthSignatureMethod.HmacSha1,
            ConsumerKey = CONSUMER_KEY,
            ConsumerSecret = CONSUMER_SECRET,
            RequestUrl = uri.ToString(),
            Version = "1.0",
            Token = _token.Token,
            TokenSecret = _token.Secret
        };
        
        _apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", client.GetAuthorizationHeader().Replace("OAuth", string.Empty).Trim());
    }

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }

        SetAuthHeader("POST", AuthUrl);

        var payload = new {
            identifier = IDENTIFIER,
            email = Config.Options.Login,
            password = Config.Options.Password
        };
        
        using var response = await _apiClient.PostAsJsonAsync(AuthUrl, payload);
        if (response.StatusCode == HttpStatusCode.OK) {
            Console.WriteLine("Успешно авторизовались");
        } else {
            var message = await response.Content.ReadAsStringAsync();
            throw new Exception($"Не удалось авторизоваться. {message}");
        }
        
        _token = await response.Content.ReadFromJsonAsync<MyBookAuth>();
    }
    
    private static T GetNextData<T>(HtmlDocument doc, string node) {
        var json = doc.QuerySelector("#__NEXT_DATA__").InnerText;
        return JsonDocument.Parse(json)
            .RootElement.GetProperty("props")
            .GetProperty("initialProps")
            .GetProperty("pageProps")
            .GetProperty(node)
            .GetRawText()
            .Deserialize<T>();
    }

    public override async Task<Book> Get(Uri url) {
        url = GetMainUrl(url);
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        var details = GetNextData<MyBookBook>(doc, "book");
        
        var book = new Book(url) {
            Cover = await GetCover(details),
            Chapters = await FillChapters(details),
            Title = details.Name,
            Author = GetAuthor(details),
            Annotation = details.Annotation,
        };

        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(MyBookBook details) {
        var chapters = new List<Chapter>();
        
        var bookUrl = SystemUrl.MakeRelativeUri(details.BookFile);
        
        SetAuthHeader("GET", bookUrl);
        using var response = await _apiClient.GetAsync(bookUrl);
        if (response.StatusCode != HttpStatusCode.OK) {
            throw new Exception("Не удалось получить книгу");
        }
        
        var book = EpubReader.Read(await response.Content.ReadAsStreamAsync(), false, Encoding.UTF8);
        var current = book.TableOfContents.First();
        
        do {
            Console.WriteLine($"Загружаю главу {current.Title.CoverQuotes()}");
            
            var chapter = new Chapter {
                Title = current.Title
            };

            var content = GetContent(book, current);
            chapter.Content = content.DocumentNode.RemoveNodes("h1, h2, h3").InnerHtml;
            chapters.Add(chapter);
        } while ((current = current.Next) != default);

        return chapters;
    }

    private static HtmlDocument GetContent(EpubBook book, EpubChapter epubChapter) {
        var nextHash = epubChapter.Next?.HashLocation;
        var sb = new StringBuilder();

        var content = book.Resources.Html.First(t => t.Href == epubChapter.RelativePath).TextContent.AsHtmlDoc();
        var start = content.QuerySelector($"#{epubChapter.HashLocation}");
        while (true) {
            if (start == default || start.Id == nextHash) {
                break;
            }
            
            sb.Append(start.OuterHtml.HtmlDecode());
            start = start.NextSibling;
        }

        var doc = sb.AsHtmlDoc();
        foreach (var node in doc.QuerySelectorAll("p")) {
            node.Attributes.RemoveAll();
        }
        
        return doc;
    }

    private Task<Image> GetCover(MyBookBook book) {
        return !string.IsNullOrWhiteSpace(book.Cover) ? SaveImage($"https://i3.mybook.io/p/x378/{book.Cover.TrimStart('/')}".AsUri()) : Task.FromResult(default(Image));
    }
    
    private Author GetAuthor(MyBookBook book) {
        if (book.Authors.Length == 0) {
            return new Author("MyBook");
        }

        var author = book.Authors[0];
        return string.IsNullOrWhiteSpace(author.Url) ? new Author(author.Name) : new Author(author.Name, SystemUrl.MakeRelativeUri(author.Url));
    }
    
    public new void Dispose() {
        base.Dispose();
        _apiClient?.Dispose();
    }
}