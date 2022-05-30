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
        var uri = new Uri($"https://litgorod.ru/books/view/{GetId(url)}");
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(uri);

        var book = new Book(uri) {
            Cover = await GetCover(doc, uri),
            Chapters = await FillChapters(doc, uri),
            Title = doc.GetTextBySelector("p.info_title"),
            Author = GetAuthor(doc, uri),
            Annotation = doc.QuerySelector("div.annotation_footer--content p.item_info")?.InnerHtml,
            Seria = GetSeria(doc)
        };
            
        return book;
    }

    public override async Task Init() {
        _config.Client.Timeout = TimeSpan.FromSeconds(30);
        
        var response = await _config.Client.GetWithTriesAsync(new Uri("https://litgorod.ru/"));
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
        
        _config.Client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
        _config.Client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        _config.Client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);
        _config.Client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", xsrf);
    }

    public override async Task Authorize() {
        if (!_config.HasCredentials) {
            return;
        }

        var payload = new {
            email = _config.Login,
            password = _config.Password
        };

        var response = await _config.Client.PostAsJsonAsync(new Uri("https://litgorod.ru/login"), payload);
        var data = await response.Content.ReadFromJsonAsync<LitgorodAuthResponse>();
        if (string.IsNullOrWhiteSpace(data?.Message)) {
            Console.WriteLine("Успешно авторизовались");
        } else {
            throw new Exception($"Не удалось авторизоваться. {data.Message}");
        }
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("a.info_author");
        return new Author(a.GetTextBySelector(), new Uri(url, a.Attributes["href"].Value));
    }

    private static Seria GetSeria(HtmlDocument doc) {
        var text = doc.GetTextBySelector("p.info_desciption--circle");
        if (!string.IsNullOrWhiteSpace(text) && text.StartsWith("Цикл")) {
            var circleName = text[4..].Trim();
            if (!string.IsNullOrWhiteSpace(circleName)) {
                return new Seria {
                    Name = circleName
                };
            }
        }

        return default;
    }
    
    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri uri) {
        var result = new List<Chapter>();

        foreach (var bookChapter in GetToc(doc, uri)) {
            var chapter = new Chapter();
            Console.WriteLine($"Загружаю главу {bookChapter.Title.CoverQuotes()}");
            
            var chapterDoc = await GetChapter(bookChapter.Url);

            if (chapterDoc != default) {
                chapter.Title = bookChapter.Title;
                chapter.Images = await GetImages(chapterDoc, uri);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;

                result.Add(chapter);
            }
        }

        return result;
    }
    
    private async Task<HtmlDocument> GetChapter(Uri url) {
        var response = await _config.Client.GetWithTriesAsync(url);
        if (response == default) {
            return default;
        }

        var doc = await response.Content.ReadAsStringAsync().ContinueWith(t => t.Result.AsHtmlDoc());
        var li = doc.QuerySelectorAll("ul.reader__pagen__ul__wrap li");
        var pages = int.Parse(li.Count > 0 ? li.Last().InnerText : "1");

        var sb = new StringBuilder();
        for (var i = 1; i <= pages; i++) {
            doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri(url + $"&page={i}"));
            sb.Append(doc.QuerySelector("div.reader__content__wrap").RemoveNodes("div.reader__content__title").InnerHtml.HtmlDecode());
        }

        return sb.AsHtmlDoc();
    }

    private static IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        return doc
            .QuerySelectorAll("div.item-2 ul a")
            .Select(a => new UrlChapter(new Uri(url, a.Attributes["href"].Value), a.GetTextBySelector()));
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.annotation_main--poster img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, imagePath)) : Task.FromResult(default(Image));
    }
}