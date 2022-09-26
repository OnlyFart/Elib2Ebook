using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Common;
using Elib2Ebook.Types.Rulate;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class RulateGetter : GetterBase {
    public RulateGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://tl.rulate.ru");

    protected override string GetId(Uri url) => url.Segments.Length == 3 ? base.GetId(url) : url.GetSegment(2);

    public override async Task Init() {
        await base.Init();
        Config.Client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        Config.CookieContainer.Add(SystemUrl, new Cookie("mature", "c3a2ed4b199a1a15f5a5483504c7a75a7030dc4bi%3A1%3B"));
    }

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }


        var doc = await Config.Client.PostHtmlDocWithTriesAsync(SystemUrl, GetAuthData());
        var alertBlock = doc.QuerySelector("div.alert-error");
        
        if (alertBlock == default) {
            Console.WriteLine("Успешно авторизовались");
        } else {
            throw new Exception($"Не удалось авторизоваться. {alertBlock.GetText()}"); 
        }
    }
    
    private FormUrlEncodedContent GetAuthData() {
        var data = new Dictionary<string, string> {
            ["login[login]"] = Config.Options.Login,
            ["login[pass]"] = Config.Options.Password,
        };

        return new FormUrlEncodedContent(data);
    }

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = SystemUrl.MakeRelativeUri($"/book/{bookId}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url, bookId),
            Title = GetTitle(doc),
            Author = GetAuthor(doc),
            Annotation = doc.QuerySelector("#Info div.btn-toolbar + div")?.InnerHtml
        };
            
        return book;
    }

    private static string GetTitle(HtmlDocument doc) {
        var match = Regex.Match(doc.ParsedText, "t_title: '(?<title>.*?)',");
        return match.Success ? match.Groups["title"].Value : doc.GetTextBySelector("h1");
    }

    private Author GetAuthor(HtmlDocument doc) {
        var def = new Author("rulate");
        foreach (var p in doc.QuerySelectorAll("#Info p")) {
            var strong = p.QuerySelector("strong");
            if (strong != null && strong.InnerText.Contains("Автор")) {
                var em = p.QuerySelector("em");
                if (em == default) {
                    return def;
                }
                
                var a = em.QuerySelector("a[href]");
                return a == default ? 
                    new Author(em.GetText()) : 
                    new Author(a.GetText(), SystemUrl.MakeRelativeUri(a.Attributes["href"].Value));

            }
        }

        return def;
    }
        
    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.slick img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
        
    private async Task<List<Chapter>> FillChapters(HtmlDocument doc, Uri bookUri, string bookId) {
        var result = new List<Chapter>();
            
        foreach (var (id, name) in GetToc(doc)) {
            Console.WriteLine($"Загружаю главу {name.CoverQuotes()}");
            var chapter = new Chapter();
                
            var chapterDoc = await GetChapter(bookId, id);
            chapter.Images = await GetImages(chapterDoc, bookUri);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = name;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(string bookId, string chapterId) {
        var s = await Config.Client.GetFromJsonWithTriesAsync<RulateChapter>(SystemUrl.MakeRelativeUri($"/book/{bookId}/{chapterId}/readyajax"));
        return (s.CanRead ? s.Content.AsHtmlDoc().QuerySelector("div.content-text")?.InnerHtml ?? string.Empty : string.Empty).AsHtmlDoc();
    }

    private IEnumerable<IdChapter> GetToc(HtmlDocument doc) {
        var result = doc.QuerySelectorAll("#Chapters tr[data-id]")
            .Select(chapter => new IdChapter(chapter.Attributes["data-id"].Value, chapter.GetTextBySelector("td.t")))
            .ToList();
        
        return SliceToc(result);
    }
}