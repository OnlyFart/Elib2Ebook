using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Misc;
using Core.Types.Book;
using Core.Types.Common;
using Core.Types.MyBook;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;
using OAuth;

namespace Core.Logic.Getters; 

public class MyBookGetter : GetterBase {
    public MyBookGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://mybook.ru/");

    private const string CONSUMER_KEY = "830968793b2a44c688400319f4a77231";
    private const string CONSUMER_SECRET = "HhaxXsMryrLz49R4";
    private const string IDENTIFIER = "47b17a4c8231a28f06c6c836d0b5f6f2134cb74d";

    private HttpClient _apiClient;
    private MyBookAuth _token = new() {
        Secret = "fYFPndYhOTW6YxIJ",
        Token = "f8d32ef906664ed3a1525b7298aac461"
    };

    private Uri GetMainUrl(Uri uri) {
        return SystemUrl.MakeRelativeUri($"{uri.GetSegment(1)}/{uri.GetSegment(2)}/{uri.GetSegment(3)}");
    }

    public override Task Init() {
        _apiClient = new HttpClient();
        _apiClient.Timeout = Config.Client.Timeout;
        _apiClient.DefaultRequestHeaders.Add("User-Agent", "MyBook/6.6.0 (iPhone; iOS 16.0.3; Scale/3.00)");
        _apiClient.DefaultRequestHeaders.Add("Accept", "application/json; version=4");
        return Task.CompletedTask;
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
        
        var authUrl = SystemUrl.MakeRelativeUri("/api/auth/");
        SetAuthHeader("POST", authUrl);

        var payload = new {
            identifier = IDENTIFIER,
            email = Config.Options.Login,
            password = Config.Options.Password
        };
        
        using var response = await _apiClient.PostAsJsonAsync(authUrl, payload);
        if (response.StatusCode == HttpStatusCode.OK) {
            Config.Logger.LogInformation("Успешно авторизовались");
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

    private async Task<MyBookBook> GetDetails(Uri url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        var details = GetNextData<MyBookBook>(doc, "book");

        if (details.Type == "audio") {
            var bookId = details.Connected?.Id ?? details.MapFiles.FirstOrDefault(b => b.Book != default)?.Book;
            if (bookId.HasValue) {
                return await GetDetailsApi(bookId.Value);
            }
            
            throw new Exception("Указана ссылка на аудиокнигу. Укажите ссылку на текстовую версию");
        }

        return details;
    }

    private async Task<MyBookBook> GetDetailsApi(long bookId) {
        var apiUrl = SystemUrl.MakeRelativeUri($"/api/v1/books/{bookId}/");
        SetAuthHeader("GET", apiUrl);
        return await _apiClient.GetFromJsonAsync<MyBookBook>(apiUrl);
    }

    public override async Task<Book> Get(Uri url) {
        url = GetMainUrl(url);
        var details = await GetDetails(url);
        
        var book = new Book(url) {
            Cover = await GetCover(details),
            Title = details.Name,
            Author = GetAuthor(details),
            Annotation = details.Annotation,
        };
        
        var bookUrl = SystemUrl.MakeRelativeUri(details.BookFile);
        
        SetAuthHeader("GET", bookUrl);
        using var response = await _apiClient.GetAsync(bookUrl);
        if (response.StatusCode != HttpStatusCode.OK) {
            throw new Exception("Не удалось получить книгу");
        }

        var origBook = await TempFile.Create(bookUrl, Config.TempFolder.Path, bookUrl.GetFileName(), await response.Content.ReadAsStreamAsync());

        if (Config.Options.HasAdditionalType(AdditionalTypeEnum.Books)) {
            book.AdditionalFiles.Add(AdditionalTypeEnum.Books, origBook);
        }

        book.Chapters = await FillChaptersFromEpub(origBook);

        if (Config.Options.HasAdditionalType(AdditionalTypeEnum.Audio) && details.Connected is { Type: "audio" }) {
            book.AdditionalFiles.Add(AdditionalTypeEnum.Audio, await GetAudio(details.Connected.Id));
        }
        
        return book;
    }

    private async Task<List<TempFile>> GetAudio(long bookId) {
        var result = new List<TempFile>();
        var details = await GetDetailsApi(bookId);
        var files = details.Files.Where(node => node is JsonObject).Select(node => node.Deserialize<MyBookFile>()).ToList();

        for (int i = 0; i < files.Count; i++) {
            var file = files[i];
            var url = SystemUrl.MakeRelativeUri(file.Url);

            Config.Logger.LogInformation($"Загружаю аудиофайл {i + 1}/{files.Count} {url}");
            SetAuthHeader("GET", url);

            var name = file.Title;
            var ext = Path.GetExtension(url.GetFileName());
            
            using var response = await _apiClient.GetAsync(url);
            result.Add(await TempFile.Create(url, Config.TempFolder.Path, $"{file.Order}_{name}{ext}", await response.Content.ReadAsStreamAsync()));
            Config.Logger.LogInformation($"Аудиофайл файл {i + 1}/{files.Count} {url} загружен");
        }
        
        return result;
    }

    private Task<TempFile> GetCover(MyBookBook book) {
        return !string.IsNullOrWhiteSpace(book.Cover) ? SaveImage($"https://i3.mybook.io/p/x378/{book.Cover.TrimStart('/')}".AsUri()) : Task.FromResult(default(TempFile));
    }
    
    private Author GetAuthor(MyBookBook book) {
        if (book.Authors.Length == 0) {
            return new Author("MyBook");
        }

        var author = book.Authors[0];
        return string.IsNullOrWhiteSpace(author.Url) ? new Author(author.Name) : new Author(author.Name, SystemUrl.MakeRelativeUri(author.Url));
    }
    
    public override void Dispose() {
        base.Dispose();
        _apiClient?.Dispose();
    }
}