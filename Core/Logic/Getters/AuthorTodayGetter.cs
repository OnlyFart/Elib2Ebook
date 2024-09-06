using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.AuthorToday;
using Core.Types.Book;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters;

public class AuthorTodayGetter : GetterBase {
    public AuthorTodayGetter(BookGetterConfig config) : base(config) { }

    protected override Uri SystemUrl => new("https://author.today/");
    
    private Uri _apiUrl => new("https://api.author.today/");
    
    /// <summary>
    ///  IP сайта author.today
    /// </summary>
    private Uri _systemIp => new("https://185.26.98.195/");

    /// <summary>
    /// IP сайта api.author.today
    /// </summary>
    private Uri _apiIp => new("https://185.26.98.195/");
    
    private bool _bypass;

    private Uri ApiUrl => _bypass ? _apiIp : _apiUrl;

    private Uri SiteUrl => _bypass ? _systemIp : SystemUrl;

    private string UserId { get; set; } = string.Empty;

    protected override string GetId(Uri url) => url.GetSegment(2);

    public override async Task Init() {
        await base.Init();

        var response = await Config.Client.GetAsync(_apiUrl);
        _bypass = response.StatusCode != HttpStatusCode.OK;
        Config.Logger.LogInformation(_bypass ? 
            $"Сайт {_apiUrl} не доступен. Работаю через {_apiIp}" : 
            $"Сайт {_apiUrl} доступен. Работаю через него");

        Config.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "guest");
    }
    
    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }
        
        var response = await Config.Client.SendAsync(GetDefaultMessage(ApiUrl.MakeRelativeUri("/v1/account/login-by-password"), _apiUrl, JsonContent.Create(new { Config.Options.Login, Config.Options.Password })));
        var data = await response.Content.ReadFromJsonAsync<AuthorTodayAuthResponse>();

        if (!string.IsNullOrWhiteSpace(data?.Token)) {
            Config.Logger.LogInformation("Успешно авторизовались");
            Config.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.Token);

            response = await Config.Client.SendWithTriesAsync(() => GetDefaultMessage(ApiUrl.MakeRelativeUri("/v1/account/current-user"), _apiUrl));
            var user = await response.Content.ReadFromJsonAsync<AuthorTodayUser>();
            UserId = user!.Id.ToString();
        } else {
            throw new Exception($"Не удалось авторизоваться. {data?.Message}");
        }
    }

    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri($"/work/{GetId(url)}");
        var details = await GetBookDetails(GetId(url));

        var book = new Book(url) {
            Cover = await GetCover(details),
            Chapters = await FillChapters(details),
            Title = details.Title,
            Author = GetAuthor(details),
            CoAuthors = GetCoAuthors(details),
            Annotation = details.Annotation,
            Seria = GetSeria(details)
        };

        return book;
    }

    private HttpRequestMessage GetDefaultMessage(Uri uri, Uri host, HttpContent content = null) {
        var message = new HttpRequestMessage(content == default ? HttpMethod.Get : HttpMethod.Post, uri);
        message.Content = content;
        message.Version = Config.Client.DefaultRequestVersion;
        
        foreach (var header in Config.Client.DefaultRequestHeaders) {
            message.Headers.Add(header.Key, header.Value);
        }

        message.Headers.Host = host.Host;

        return message;
    }

    private async Task<AuthorTodayBookDetails> GetBookDetails(string bookId) {
        var response = await Config.Client.SendWithTriesAsync(() => GetDefaultMessage(ApiUrl.MakeRelativeUri($"/v1/work/{bookId}/details"), _apiUrl));
        if (response.StatusCode != HttpStatusCode.OK) {
            throw new Exception("Книга не найдена");
        }

        return await response.Content.ReadFromJsonAsync<AuthorTodayBookDetails>();
    }

    private Author GetAuthor(AuthorTodayBookDetails book) {
        return new Author(book.AuthorFio, SystemUrl.MakeRelativeUri($"/u/{book.AuthorUserName}/works"));
    }
    
    private IEnumerable<Author> GetCoAuthors(AuthorTodayBookDetails book) {
        var result = new List<Author>();
        if (!string.IsNullOrWhiteSpace(book.CoAuthorFio)) {
            result.Add( new Author(book.CoAuthorFio, SystemUrl.MakeRelativeUri($"/u/{book.CoAuthorUserName}/works")));
        }

        return result;
    }

    private Seria GetSeria(AuthorTodayBookDetails book) {
        if (!book.SeriesId.HasValue) {
            return default;
        }

        return new Seria {
            Name = book.SeriesTitle,
            Number = book.SeriesWorkNumber.HasValue ? book.SeriesWorkNumber.ToString() : string.Empty,
            Url = SystemUrl.MakeRelativeUri($"/work/series/{book.SeriesId}")
        };
    }

    private Task<Image> GetCover(AuthorTodayBookDetails book) {
        return !string.IsNullOrWhiteSpace(book.CoverUrl) ? SaveImage(book.CoverUrl.AsUri()) : Task.FromResult(default(Image));
    }

    protected override HttpRequestMessage GetImageRequestMessage(Uri uri) {
        if (uri.IsSameHost(SystemUrl) || uri.IsSameSubDomain(SystemUrl)) {
            return GetDefaultMessage(SiteUrl.MakeRelativeUri(uri.AbsolutePath), uri);
        }

        return base.GetImageRequestMessage(uri);
    }

    private async Task<IEnumerable<Chapter>> FillChapters(AuthorTodayBookDetails book) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }
        
        foreach (var atChapter in await GetChapters(book)) {
            var title = atChapter.Title.ReplaceNewLine();
            Config.Logger.LogInformation($"Загружаю главу {title.CoverQuotes()}");
            
            var chapter = new Chapter {
                Title = title
            };

            if (atChapter.IsSuccessful) {
                var chapterDoc = atChapter.Decode(UserId).AsHtmlDoc();
                chapter.Images = await GetImages(chapterDoc, SystemUrl);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }

            result.Add(chapter);
        }
            
        return result;
    }

    private async Task<IEnumerable<AuthorTodayChapter>> GetChapters(AuthorTodayBookDetails book) {
        var result = new List<AuthorTodayChapter>();
        
        foreach (var chunk in book.Chapters.Where(c => !c.IsDraft).OrderBy(c => c.SortOrder).Chunk(100)) {
            var ids = string.Join("&", chunk.Select((c, i) => $"ids[{i}]={c.Id}"));
            var uri = ApiUrl.MakeRelativeUri($"/v1/work/{book.Id}/chapter/many-texts?{ids}");
            var response = await Config.Client.SendWithTriesAsync(() => GetDefaultMessage(uri, _apiUrl));
            var chapters = await response.Content.ReadFromJsonAsync<AuthorTodayChapter[]>();
            if (chapters != default) {
                result.AddRange(chapters.Where(c => c.Code != "NotFound"));
            }
        }

        return SliceToc(result);
    }
}