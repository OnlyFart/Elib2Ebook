using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using Core.Types.Litexit;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class IGramGetter(BookGetterConfig config) : GetterBase(config) {
    protected override Uri SystemUrl => new("https://i-gram.ru/");

    protected override string GetId(Uri url) => url.GetSegment(2);

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri("/account/login/"));
        var token = doc.QuerySelector("input[name=csrfmiddlewaretoken]")?.Attributes["value"]?.Value;
        
        Config.Client.DefaultRequestHeaders.Add("Referer", SystemUrl.MakeRelativeUri("/account/login/").ToString());
        using var post = await Config.Client.PostAsync(SystemUrl.MakeRelativeUri("/account/login/"), GenerateAuthData(token));
        var checkLogin = await Config.Client.GetFromJsonAsync<LitexitUser>(SystemUrl.MakeRelativeUri("/api/v2/users/current/"));
        if (checkLogin?.Id > 0) {
            Config.Logger.LogInformation("Успешно авторизовались");
        } else {
            throw new Exception("Не удалось авторизоваться");
        }
    }

    private FormUrlEncodedContent GenerateAuthData(string token) {
        var data = new Dictionary<string, string> {
            ["csrfmiddlewaretoken"] = token,
            ["remember"] = "checked",
            ["login"] = Config.Options.Login,
            ["password"] = Config.Options.Password
        };

        return new FormUrlEncodedContent(data);
    }

    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri($"/b/{GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        
        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(url, doc),
            Title = doc.GetTextBySelector("h1[itemprop=name]"),
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div[itemprop=description] p")?.InnerHtml,
            Seria = GetSeria(doc, url)
        };
            
        return book;
    }

    private static Seria GetSeria(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("div.bk-genre a[href^=/b/cycle/]");
        if (a != default) {
            return new Seria {
                Name = a.GetText(),
                Url = url.MakeRelativeUri(a.Attributes["href"].Value)
            };
        }

        return default;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(Uri uri, HtmlDocument doc) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        foreach (var bookChapter in await GetToc(doc)) {
            Config.Logger.LogInformation($"Загружаю главу {bookChapter.Title.CoverQuotes()}");
            var chapter = new Chapter {
                Title = bookChapter.Title
            };
            
            if (!string.IsNullOrWhiteSpace(bookChapter.Text)) {
                var chapterDoc = bookChapter.Text.AsHtmlDoc();
                chapter.Images = await GetImages(chapterDoc, uri);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }
            
            result.Add(chapter);
        }

        return result;
    }

    private async Task<IEnumerable<LitexitChapter>> GetToc(HtmlDocument doc) {
        var response = await Config.Client.GetAsync(SystemUrl.MakeRelativeUri($"/api/v1/book/{GetInternalId(doc)}/chapters"));

        var content = await response.Content.ReadAsStringAsync();
        return SliceToc(content.Deserialize<IEnumerable<LitexitChapter>>().OrderBy(c => c.Id).ToList(), c => c.Title);
    }

    private static Author GetAuthor(HtmlDocument doc, Uri uri) {
        var a = doc.QuerySelector("div.bk-author a");
        return new Author(a.GetText(), uri.MakeRelativeUri(a.Attributes["href"].Value));
    }

    private static string GetInternalId(HtmlDocument doc) {
        return Regex.Match(doc.DocumentNode.InnerHtml, @"bookId: '(?<id>\d+)'").Groups["id"].Value;
    }

    private Task<TempFile> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.bk-img img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
}