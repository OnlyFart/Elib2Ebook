using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using Core.Types.FanFicus;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters;

public class FanFicusGetter(BookGetterConfig config) : GetterBase(config) {
    protected override Uri SystemUrl => new("https://fanficus.com/");
    
    private readonly Uri _apiHost = new("https://fanficus-server-mirror-879c30cd977f.herokuapp.com/");

    protected override string GetId(Uri url) {
        return url.GetSegment(2);
    }

     public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }
        
        var response = await Config.Client.PostAsJsonAsync(_apiHost.MakeRelativeUri("api/v1/user/login"), GenerateAuthData());
        if (response.StatusCode == HttpStatusCode.OK) {
            var user = await response.Content.ReadFromJsonAsync<FanFicusApiResponse<FanFicusUser>>();
            Config.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Value.Token);
            Config.Logger.LogInformation("Успешно авторизовались");
        } else {
            throw new Exception("Не удалось авторизоваться. Неверный пароль или имейл");
        }
    }

    private object GenerateAuthData() {
        return new {
             username = Config.Options.Login,
             password = Config.Options.Password,
        };
    }

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        
        url = SystemUrl.MakeRelativeUri($"/post/{bookId}");
        var response = await Config.Client.GetFromJsonWithTriesAsync<FanFicusApiResponse<FanFicusBook>>(_apiHost.MakeRelativeUri($"api/v1/post/{bookId}"));
        
        var book = new Book(url) {
            Cover = await GetCover(response.Value, url),
            Chapters = await FillChapters(bookId),
            Title = response.Value.Title,
            Author = GetAuthor(response.Value),
            Annotation = response.Value.Description
        };
            
        return book;
    }

    private Author GetAuthor(FanFicusBook book) {
        var creator = book.Creators.FirstOrDefault();
        return creator == default ? new Author("FanFicus") : new Author(creator.NickName, SystemUrl.MakeRelativeUri($"/user/{creator.Id}"));
    }

    private async Task<IEnumerable<FanFicusPart>> GetToc(string bookId) {
        var response = await Config.Client.GetFromJsonWithTriesAsync<FanFicusApiResponse<FanFicusPart[]>>(_apiHost.MakeRelativeUri($"/api/v1/post/{bookId}/post-part"));
        return SliceToc(response.Value, c => c.Title);
    }
    
    private async Task<IEnumerable<Chapter>> FillChapters(string bookId) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        foreach (var fanFicusPart in await GetToc(bookId)) {
            var chapter = new Chapter {
                Title = fanFicusPart.Title
            };

            Config.Logger.LogInformation($"Загружаю главу {fanFicusPart.Title.CoverQuotes()}");

            var chapterDoc = await GetChapter(bookId, fanFicusPart);
            if (chapterDoc != default) {
                chapter.Images = await GetImages(chapterDoc, SystemUrl);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }
            
            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(string bookId, FanFicusPart fanFicusPart) {
        var response = await Config.Client.GetFromJsonWithTriesAsync<FanFicusApiResponse<FanFicusChapter>>(_apiHost.MakeRelativeUri($"api/v1/post/{bookId}/post-part/{fanFicusPart.Id}"));
        return response.Value.Text.AsHtmlDoc();
    }

    private Task<TempFile> GetCover(FanFicusBook doc, Uri uri) {
        var imagePath = doc.Images.FirstOrDefault()?.Url;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
}