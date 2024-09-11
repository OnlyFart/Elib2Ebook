using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Bookstime;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class BookstimeGetter : GetterBase {
    public BookstimeGetter(BookGetterConfig config) : base(config) { }
    
    protected override Uri SystemUrl => new("https://bookstime.ru/");

    protected override string GetId(Uri url) {
        return url.GetSegment(2);
    }

    public override async Task Init() {
        await base.Init();
        Config.Client.DefaultRequestHeaders.Add("X-OCTOBER-REQUEST-FLASH", "1");
        Config.Client.DefaultRequestHeaders.Add("X-OCTOBER-REQUEST-HANDLER", "bookAccount::onSignin");
        Config.Client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
    }

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }
        
        using var post = await Config.Client.PostAsync(SystemUrl, GenerateAuthData());
        if (post.StatusCode != HttpStatusCode.OK) {
            var content = await post.Content.ReadAsStringAsync();
            throw new Exception($"Не удалось авторизоваться. {content}");
        }
        
        var response = await post.Content.ReadFromJsonAsync<BookstimeAuthResponse>();
        if (string.IsNullOrWhiteSpace(response.XOctoberErrorMessage)) {
            Config.Logger.LogInformation("Успешно авторизовались");
        } else {
            throw new Exception($"Не удалось авторизоваться. {response.XOctoberErrorMessage}");
        }
    }

    private FormUrlEncodedContent GenerateAuthData() {
        var payload = new Dictionary<string, string> {
            { "email", Config.Options.Login },
            { "password", Config.Options.Password },
        };

        return new FormUrlEncodedContent(payload);
    }

    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri("/book-card/" + GetId(url));
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1"),
            Author = GetAuthor(doc),
            Annotation = doc.QuerySelector("div[itemprop=description]")?.InnerHtml
        };
            
        return book;
    }

    private Author GetAuthor(HtmlDocument doc) {
        var a = doc.QuerySelector("a.book-card-full__author");
        return new Author(a.GetText(), SystemUrl.MakeRelativeUri(a.Attributes["href"].Value));
    }
    
    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        var result = doc
            .QuerySelectorAll("div.book-card-full-description__content a")
            .Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.GetText().ReplaceNewLine()))
            .ToList();
        
        return SliceToc(result, c => c.Title);
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        foreach (var urlChapter in GetToc(doc, url)) {
            Config.Logger.LogInformation($"Загружаю главу {urlChapter.Title.CoverQuotes()}");
            var chapter = new Chapter {
                Title = urlChapter.Title
            };

            var chapterDoc = await GetChapter(urlChapter);
            if (chapterDoc != default) {
                chapter.Images = await GetImages(chapterDoc, urlChapter.Url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(UrlChapter urlChapter) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url);
        var reader = doc.QuerySelector("div.reader__body");
        if (reader == default) {
            return default;
        }
        
        var text = new StringBuilder();
        text.Append(reader.RemoveNodes("h2.ui-text-head--3-em, iframe").InnerHtml);
        
        var paging = doc
            .QuerySelectorAll("a.ui-pagination-item")
            .Where(a => int.TryParse(a.GetText(), out _))
            .Select(a => int.Parse(a.GetText()))
            .ToList();
        
        var pages = paging.Any() ? paging.Max() : 1;
        for (var i = 2; i <= pages; i++) {
            doc = await Config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url.AppendSegment(i.ToString()));
            text.Append(doc.QuerySelector("div.reader__body").RemoveNodes("h2.ui-text-head--3-em, iframe").InnerHtml);
        }
        
        return text.AsHtmlDoc();
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div[itemprop=image] img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}