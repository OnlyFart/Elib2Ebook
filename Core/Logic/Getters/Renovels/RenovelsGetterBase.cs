using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Renovels;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters.Renovels; 

public abstract class RenovelsGetterBase : GetterBase {
    protected RenovelsGetterBase(BookGetterConfig config) : base(config) { }

    private Uri _apiUrl => new($"https://api.{SystemUrl.Host}/");
    
    protected abstract string Segment { get; }
    
    protected abstract HtmlDocument GetChapterAsHtml(RenovelsApiResponse<RenovelsChapter> response);

    protected override string GetId(Uri url) => url.GetSegment(2);

    public override Task Init() {
        Config.Client.DefaultRequestHeaders.Add("User-Agent", "remanga/1.1.6 CFNetwork/1408.0.4 Darwin/22.5.0");
        Config.Client.DefaultRequestHeaders.Add("Referer", SystemUrl.ToString());
        return Task.CompletedTask;
    }

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }
        
        var payload = new {
            user = Config.Options.Login,
            password = Config.Options.Password
        };

        var response = await Config.Client.PostAsJsonAsync("https://api.recomics.org/api/users/login/".AsUri(), payload);
        var data = await response.Content.ReadFromJsonAsync<RenovelsApiResponse<JsonNode>>();
        if (string.IsNullOrWhiteSpace(data.Message)) {
            var auth = data.Content.Deserialize<RenovelsAuthResponse>();
            Config.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
            Config.Logger.LogInformation("Успешно авторизовались");
        } else {
            throw new Exception($"Не удалось авторизоваться. {data.Message}");
        }
    }

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        var content = await GetContent(bookId);

        var book = new Book(SystemUrl.MakeRelativeUri($"/{Segment}/{bookId}")) {
            Cover = await GetCover(content, url),
            Chapters = await FillChapters(content, url),
            Title = content.RusName,
            Author = GetAuthor(content),
            Annotation = content.Description
        };
            
        return book;
    }
    
    private Author GetAuthor(RenovelsContent content) {
        if (content.Publishers.Length == 0) {
            return new Author("Renovels");
        }

        var author = content.Publishers[0];
        return new Author(author.Name, SystemUrl.MakeRelativeUri($"/team/{author.Dir}"));
    }

    private async Task<RenovelsContent> GetContent(string bookId) {
        var response = await Config.Client.GetFromJsonAsync<RenovelsApiResponse<RenovelsContent>>(_apiUrl.MakeRelativeUri($"/api/titles/{bookId}/"));
        return response.Content;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(RenovelsContent content, Uri url) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }
            
        foreach (var ranobeChapter in await GetToc(content)) {
            Config.Logger.LogInformation($"Загружаю главу {ranobeChapter.Title.CoverQuotes()}");
            var chapter = new Chapter();
            var doc = await GetChapter(ranobeChapter);
            chapter.Images = await GetImages(doc, url);
            chapter.Content = doc.DocumentNode.InnerHtml;
            chapter.Title = ranobeChapter.Title;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(RenovelsChapter ranobeChapter) {
        var makeRelativeUri = _apiUrl.MakeRelativeUri($"/api/titles/chapters/{ranobeChapter.Id}/");
        var response = await Config.Client.GetFromJsonAsync<RenovelsApiResponse<RenovelsChapter>>(makeRelativeUri);
        return GetChapterAsHtml(response);
    }

    private Task<TempFile> GetCover(RenovelsContent book, Uri bookUri) {
        var imagePath = book.Img.GetValueOrDefault("high", null) ?? book.Img.FirstOrDefault().Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }

    private async Task<IEnumerable<RenovelsChapter>> GetToc(RenovelsContent content) {
        var result = new List<RenovelsChapter>();
        
        for (var i = 1;; i++) {
            var uri = _apiUrl.MakeRelativeUri($"/api/titles/chapters/?branch_id={content.Branches[0].Id}&ordering=index&user_data=1&count=40&page={i}");
            var response = await Config.Client.GetFromJsonAsync<RenovelsApiResponse<RenovelsChapter[]>>(uri);
            result.AddRange(response!.Content);

            if (response.Content.Length < 40) {
                return SliceToc(result.Where(c => !c.IsPaid || c.IsBought == true).ToList(), c => c.Name);
            }
        }
    }
}