using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Freedlit;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class FreedlitGetter : GetterBase{
    public FreedlitGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://freedlit.space/");

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }

        await Config.Client.GetAsync(SystemUrl.MakeRelativeUri("/login"));
        SetToken();
        using var post = await Config.Client.PostAsJsonAsync(SystemUrl.MakeRelativeUri("/login"), GenerateAuthData());
        var doc = await post.Content.ReadAsStreamAsync().ContinueWith(c => c.Result.AsHtmlDoc());
        var errors = doc.QuerySelector("#app").Attributes["data-page"].Value.HtmlDecode().Deserialize<FreedlitApp<FreedlitAuthError>>().Props.Errors;
        
        if (string.IsNullOrWhiteSpace(errors?.Email)) {
            Config.Logger.LogInformation("Успешно авторизовались");
        } else {
            throw new Exception($"Не удалось авторизоваться. {errors.Email}");
        }
    }

    private object GenerateAuthData() {
        return new {
            email = Config.Options.Login,
            password = Config.Options.Password,
            remember = false,
        };
    }

    private void SetToken() {
        Config.Client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        Config.Client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", HttpUtility.UrlDecode(Config.CookieContainer.GetAllCookies().FirstOrDefault(c => c.Name == "XSRF-TOKEN").Value));
    }

    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri($"/book/{GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var freedlitBook = doc.QuerySelector("#app").Attributes["data-page"].Value.HtmlDecode().Deserialize<FreedlitApp<JsonObject>>().Props.Book;
        SetToken();
        
        var book = new Book(url) {
            Cover = await GetCover(freedlitBook),
            Chapters = await FillChapters(freedlitBook),
            Title = freedlitBook.Title,
            Author = GetAuthor(freedlitBook),
            Annotation = freedlitBook.Annotation,
            Lang = freedlitBook.Language
        };
            
        return book;
    }

    private Author GetAuthor(FreedlitBook book) {
        return new Author(book.MainAuthor.Name, SystemUrl.MakeRelativeUri($"/p/{book.MainAuthor.UserLink}"));
    }
    
    private async Task<IEnumerable<FreedlitChapter>> GetToc(FreedlitBook book) {
        var response = await Config.Client.PostAsJsonAsync(SystemUrl.MakeRelativeUri("/api/bookpage/get-chapters"), new { book_id = book.Id });
        var data = await response.Content.ReadFromJsonAsync<FreedlitApiResponse<FreedlitItemsContent<FreedlitChapter>>>();
        
        return SliceToc(data.Success.Items, c => c.Header);
    }

    private async Task<IEnumerable<Chapter>> FillChapters(FreedlitBook book) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        foreach (var freedlitChapter in await GetToc(book)) {
            var chapter = new Chapter {
                Title = freedlitChapter.Header
            };

            Config.Logger.LogInformation($"Загружаю главу {freedlitChapter.Header.CoverQuotes()}");

            var chapterDoc = await GetChapter(book, freedlitChapter);
            if (chapterDoc != default) {
                chapter.Images = await GetImages(chapterDoc, SystemUrl);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(FreedlitBook book, FreedlitChapter chapter) {
        var response = await Config.Client.PostAsJsonAsync(SystemUrl.MakeRelativeUri("/reader/get-content"), new { book_id = book.Id, chapter_id = chapter.Id });
        var fullChapter = await response.Content.ReadFromJsonAsync<FreedlitApiResponse<FreedlitChapter>>();
        if (string.IsNullOrWhiteSpace(fullChapter.Success?.Content)) {
            return default;
        }
        
        var doc = fullChapter.Success.Content.AsHtmlDoc();
        return doc.QuerySelector("body").InnerHtml.AsHtmlDoc();
    }

    private Task<TempFile> GetCover(FreedlitBook book) {
        return !string.IsNullOrWhiteSpace(book.Cover) ? SaveImage(SystemUrl.MakeRelativeUri($"/storage/{book.Cover}")) : Task.FromResult(default(TempFile));
    }
}