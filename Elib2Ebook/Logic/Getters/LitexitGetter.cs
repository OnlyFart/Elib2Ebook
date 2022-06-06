using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Litexit;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class LitexitGetter : GetterBase {
    public LitexitGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://litexit.ru/");

    protected override string GetId(Uri url) {
        return url.Segments[2].Trim('/');
    }

    public override async Task Authorize() {
        if (!_config.HasCredentials) {
            return;
        }

        var doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri("https://litexit.ru/account/login/"));
        var token = doc.QuerySelector("input[name=csrfmiddlewaretoken]")?.Attributes["value"]?.Value;
        
        _config.Client.DefaultRequestHeaders.Add("Referer", "https://litexit.ru/account/login/");
        using var post = await _config.Client.PostAsync(new Uri("https://litexit.ru/account/login/"), GenerateAuthData(token));
        var checkLogin = await _config.Client.GetFromJsonAsync<LitexitUser>("https://litexit.ru/api/v2/users/current/");
        if (checkLogin?.Id > 0) {
            Console.WriteLine("Успешно авторизовались");
        } else {
            throw new Exception("Не удалось авторизоваться");
        }
    }

    private FormUrlEncodedContent GenerateAuthData(string token) {
        var data = new Dictionary<string, string> {
            ["csrfmiddlewaretoken"] = token,
            ["remember"] = "checked",
            ["login"] = _config.Login,
            ["password"] = _config.Password
        };

        return new FormUrlEncodedContent(data);
    }

    public override async Task<Book> Get(Uri url) {
        url = new Uri($"https://litexit.ru/b/{GetId(url)}");
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
        
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
                Url = new Uri(url, a.Attributes["href"].Value)
            };
        }

        return default;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(Uri uri, HtmlDocument doc) {
        var result = new List<Chapter>();

        foreach (var bookChapter in await GetToc(doc)) {
            var chapter = new Chapter();
            Console.WriteLine($"Загружаю главу {bookChapter.Title.CoverQuotes()}");

            if (!string.IsNullOrWhiteSpace(bookChapter.Text)) {
                var chapterDoc = bookChapter.Text.AsHtmlDoc();

                chapter.Title = bookChapter.Title;
                chapter.Images = await GetImages(chapterDoc, uri);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;

                result.Add(chapter);
            }
        }

        return result;
    }

    private async Task<IEnumerable<LitexitChapter>> GetToc(HtmlDocument doc) {
        var response = await _config.Client.GetAsync(new Uri($"https://litexit.ru/api/v1/book/{GetInternalId(doc)}/chapters"));

        var content = await response.Content.ReadAsStringAsync();
        return content.Deserialize<IEnumerable<LitexitChapter>>().OrderBy(c => c.Id);
    }

    private static Author GetAuthor(HtmlDocument doc, Uri uri) {
        var a = doc.QuerySelector("div.bk-author a");
        return new Author(a.GetText(), new Uri(uri, a.Attributes["href"].Value));
    }

    private static string GetInternalId(HtmlDocument doc) {
        return Regex.Match(doc.DocumentNode.InnerHtml, @"bookId: '(?<id>\d+)'").Groups["id"].Value;
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.bk-img img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, imagePath)) : Task.FromResult(default(Image));
    }
}