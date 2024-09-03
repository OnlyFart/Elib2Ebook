using System;
using System.Collections.Generic;
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
            Console.WriteLine("Успешно авторизовались");
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
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1"),
            Author = GetAuthor(doc),
            Annotation = doc.QuerySelector("div.book-informate div.item-descr")?.InnerHtml
        };
            
        return book;
    }

    private Author GetAuthor(HtmlDocument doc) {
        var a = doc.QuerySelector("div.p-book-author a");
        return new Author(a.GetText(), SystemUrl.MakeRelativeUri(a.Attributes["href"].Value));
    }
    
    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        var result = new List<UrlChapter>();
        foreach (var block in doc.QuerySelectorAll("div.card-glava div.item-center")) {
            var a = block.QuerySelector("a");
            result.Add(a == default
                ? new UrlChapter(null, block.GetTextBySelector("div.item-name span"))
                : new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.GetText().ReplaceNewLine()));
        }
        
        return SliceToc(result);
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();
            
        foreach (var urlChapter in GetToc(doc, url)) {
            Console.WriteLine($"Загружаю главу {urlChapter.Title.CoverQuotes()}");
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

    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.p-book-cover img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}