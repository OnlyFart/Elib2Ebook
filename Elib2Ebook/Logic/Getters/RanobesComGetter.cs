using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Ranobes;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Elib2Ebook.Extensions;

namespace Elib2Ebook.Logic.Getters; 

public class RanobesComGetter : GetterBase {
    public RanobesComGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ranobes.com");
    
    // cloudflare :(
    private const string HOST = "5.252.195.125";

    protected override string GetId(Uri url) {
        return base.GetId(url).Split(".")[0];
    }

    public override async Task Init() {
        await base.Init();
        _config.Client.DefaultRequestHeaders.Add("Host", "ranobes.com");
    }

    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);
        var bookId = GetId(url);
        var uri = new Uri($"https://{HOST}/ranobe/{bookId}.html");
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(uri);

        var book = new Book {
            Cover = await GetCover(doc, uri),
            Chapters = await FillChapters(doc, uri),
            Title = doc.QuerySelector("h1.title").FirstChild.InnerText.Trim().HtmlDecode(),
            Author = "Ранобэс"
        };
            
        return book;
    }

    private async Task<Uri> GetMainUrl(Uri url) {
        if (url.Segments[1] == "chapters/") {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri(new Uri($"https://{HOST}/"), url.AbsolutePath));
            return new Uri(url, doc.QuerySelector("div.category a").Attributes["href"].Value);
        }

        return url;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();

        foreach (var ranobeChapter in await GetChapters(GetTocLink(doc, url))) {
            Console.WriteLine($"Загружаем главу {ranobeChapter.Title.CoverQuotes()}");
            var chapter = new Chapter();
            var chapterDoc = await GetChapter(url, ranobeChapter.Url);
            chapter.Images = await GetImages(chapterDoc, url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = ranobeChapter.Title;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri mainUrl, Uri url) {
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri(mainUrl, url));
        var sb = new StringBuilder();
        foreach (var node in doc.QuerySelectorAll("#arrticle > :not(.splitnewsnavigation)")) {
            var tag = node.Name == "#text" ? "p" : node.Name;
            if (tag == "img") {
                sb.Append($"<{tag}>{node.OuterHtml.Trim()}</{tag}>");
            } else {
                if (node.InnerHtml?.Contains("window.yaContextCb") == false) {
                    sb.Append($"<{tag}>{node.InnerHtml.Trim()}</{tag}>");
                }
            }
        }
            
        return sb.ToString().HtmlDecode().AsHtmlDoc().RemoveNodes(t => t.Name is "script" or "br" || t.Id?.Contains("yandex_rtb") == true);
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.poster img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
    }

    private Uri GetTocLink(HtmlDocument doc, Uri uri) {
        var relativeUri = doc.QuerySelector("div.r-fullstory-btns a[title~=оглавление]").Attributes["href"].Value.HtmlDecode();
        if (!relativeUri.Contains("chapters")) {
            relativeUri = $"/chapters/{string.Join("-", GetId(uri).Split(".")[0].Split("-").Skip(1))}";
        }
        return new Uri(uri, new Uri(relativeUri).AbsolutePath);
    }
        
    private async Task<IEnumerable<RanobesChapter>> GetChapters(Uri tocUri) {
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(tocUri);
        var lastA = doc.QuerySelector("div.pages a:last-child")?.InnerText;
        var pages = string.IsNullOrWhiteSpace(lastA) ? 1 : int.Parse(lastA);
            
        Console.WriteLine("Получаем оглавление");
        var chapters = new List<RanobesChapter>();
        for (var i = 1; i <= pages; i++) {
            doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri(tocUri.AbsoluteUri + "/page/" + i));
            var ranobesChapters = doc
                .QuerySelectorAll("#dle-content > .cat_block.cat_line a")
                .Select(a => new RanobesChapter(a.Attributes["title"].Value, new Uri(new Uri($"https://{HOST}/"), new Uri(a.Attributes["href"].Value).AbsolutePath)))
                .ToList();
            
            chapters.AddRange(ranobesChapters);
        }
        Console.WriteLine($"Получено {chapters.Count} глав");

        chapters.Reverse();
        return chapters;
    }
}