using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using Core.Types.Litsovet;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class LitsovetGetter : GetterBase {
    public LitsovetGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://litsovet.ru/");
    protected override string GetId(Uri url) => url.GetSegment(2);

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }
        
        using var post = await Config.Client.PostAsync(SystemUrl.MakeRelativeUri("/ajax/login_signup_remind"), GenerateAuthData());
        var response = await post.Content.ReadFromJsonAsync<LitsovetLoginResponse>();
        if (response.Ok == 1) {
            Config.Logger.LogInformation("Успешно авторизовались");
        } else {
            throw new Exception("Не удалось авторизоваться.");
        }
    }

    private FormUrlEncodedContent GenerateAuthData() {
        var payload = new Dictionary<string, string> {
            { "action", "login" },
            { "email", Config.Options.Login },
            { "password", Config.Options.Password },
        };

        return new FormUrlEncodedContent(payload);
    }

    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri("/books/" + GetId(url));
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(url),
            Title = doc.GetTextBySelector("h1"),
            Author = GetAuthor(doc),
            Annotation = doc.QuerySelector("div.book-informate div.item-descr")?.InnerHtml
        };
            
        return book;
    }

    private Author GetAuthor(HtmlDocument doc) {
        var a = doc.QuerySelector("div.p-book-author a");
        return new Author(a.QuerySelector("span").GetText(), SystemUrl.MakeRelativeUri(a.Attributes["href"].Value));
    }
    
    private async Task<IEnumerable<UrlChapter>> GetToc(Uri url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url.AppendSegment("/read/"));
        
        var result = new List<UrlChapter>();
        foreach (var a in doc.QuerySelectorAll("div.book-navi-list li a")) {
            var href = a.Attributes["onclick"]?.Value?.Split(" ").FirstOrDefault(s => s.Contains("books"))?.Trim('\'');
            result.Add(string.IsNullOrEmpty(href)
                ? new UrlChapter(null, a.GetText().ReplaceNewLine())
                : new UrlChapter(url.MakeRelativeUri(href), a.GetText().ReplaceNewLine()));
        }
        
        return SliceToc(result, c => c.Title);
    }

    private async Task<IEnumerable<Chapter>> FillChapters(Uri url) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }
            
        foreach (var urlChapter in await GetToc(url)) {
            Config.Logger.LogInformation($"Загружаю главу {urlChapter.Title.CoverQuotes()}");
            var chapter = new Chapter {
                Title = urlChapter.Title
            };

            if (urlChapter.Url != default) {
                var chapterDoc = await GetChapter(urlChapter);
                chapter.Images = await GetImages(chapterDoc, urlChapter.Url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(UrlChapter urlChapter) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url);
        return doc.QuerySelector("div.reader").InnerHtml.AsHtmlDoc();
    }

    private Task<TempFile> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.p-book-cover img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
}