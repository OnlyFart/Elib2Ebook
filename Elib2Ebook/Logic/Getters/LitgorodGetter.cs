using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Common;
using Elib2Ebook.Types.Litgorod;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class LitgorodGetter : GetterBase {
    public LitgorodGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://litgorod.ru/");
    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri($"/books/view/{GetId(url)}");
        Config.Client.DefaultRequestHeaders.Add("Referer", url.ToString());
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("div.b-book_item__name h1"),
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div[data-tab-item=1] > p")?.InnerHtml,
            Seria = GetSeria(doc, url)
        };
            
        return book;
    }

    public override async Task Init() {
        var response = await Config.Client.GetWithTriesAsync(SystemUrl);
        var doc = await response.Content.ReadAsStringAsync().ContinueWith(t => t.Result.AsHtmlDoc());
        
        var csrf = doc.QuerySelector("[name=csrf-token]")?.Attributes["content"]?.Value;
        if (string.IsNullOrWhiteSpace(csrf)) {
            throw new ArgumentException("Не удалось получить csrf-token", nameof(csrf));
        }
            
        var cookies = response.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;
        var xsrfCookie = cookies.FirstOrDefault(c => c.StartsWith("XSRF-TOKEN="));
        if (xsrfCookie == null) {
            throw new ArgumentException("Не удалось получить XSRF-TOKEN", nameof(xsrfCookie));
        }
            
        var xsrf = HttpUtility.UrlDecode(xsrfCookie.Split(";")[0].Replace("XSRF-TOKEN=", ""));
        
        Config.Client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
        Config.Client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        Config.Client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);
        Config.Client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", xsrf);
    }

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }

        var payload = new {
            email = Config.Options.Login,
            password = Config.Options.Password
        };

        var response = await Config.Client.PostAsJsonAsync(SystemUrl.MakeRelativeUri("login"), payload);
        var data = await response.Content.ReadFromJsonAsync<LitgorodAuthResponse>();
        if (string.IsNullOrWhiteSpace(data?.Message)) {
            Console.WriteLine("Успешно авторизовались");
        } else {
            throw new Exception($"Не удалось авторизоваться. {data.Message}");
        }
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("div.b-book_item__author a");
        return new Author(a.GetText(), url.MakeRelativeUri(a.Attributes["href"].Value));
    }

    private static Seria GetSeria(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("div.b-book_item__cycle a");
        if (a != default) {
            return new Seria {
                Name = a.GetText(),
                Url = url.MakeRelativeUri(a.Attributes["href"].Value)
            };
        }

        return default;
    }
    
    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri uri) {
        var result = new List<Chapter>();

        foreach (var bookChapter in GetToc(doc, uri)) {
            var chapter = new Chapter {
                Title = bookChapter.Title
            };

            Console.WriteLine($"Загружаю главу {bookChapter.Title.CoverQuotes()}");
            var chapterDoc = await GetChapter(bookChapter.Url);

            if (chapterDoc != default) {
                chapter.Images = await GetImages(chapterDoc, uri);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }
            
            result.Add(chapter);
        }

        return result;
    }
    
    private async Task<HtmlDocument> GetChapter(Uri url) {
        var response = await Config.Client.GetWithTriesAsync(url);
        if (response == default) {
            return default;
        }

        var doc = await response.Content.ReadAsStreamAsync().ContinueWith(t => t.Result.AsHtmlDoc());

        var content = doc.QuerySelector("book-reader");
        if (content == default) {
            return default;
        }

        var sb = new StringBuilder();

        var chapterContent = doc.QuerySelector("book-reader").Attributes[":current_chapter"].Value;
        foreach (var paragraph in HttpUtility.HtmlDecode(chapterContent).Deserialize<LitgorodChapter>().Paragraphs) {
            sb.Append(paragraph.HtmlDecode());
        }

        return sb.AsHtmlDoc();
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        var result = doc
            .QuerySelectorAll("div.b-tab__content ul.list-unstyled a")
            .Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), string.IsNullOrWhiteSpace(a.GetText()) ? "Без названия" : a.GetText()))
            .ToList();
        
        return SliceToc(result);
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.b-book_cover a[href]")?.Attributes["href"]?.Value ?? doc.QuerySelector("div.b-book_cover img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}