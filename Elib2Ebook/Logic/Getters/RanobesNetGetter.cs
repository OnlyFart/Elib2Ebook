using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Common;
using Elib2Ebook.Types.RanobesNet;

namespace Elib2Ebook.Logic.Getters; 

public class RanobesNetGetter : GetterBase {
    public RanobesNetGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ranobes.net");
    
    // cloudflare :(
    private const string HOST = "5.252.195.125";

    protected override string GetId(Uri url) {
        return base.GetId(url).Split(".")[0];
    }

    public override async Task Init() {
        await base.Init();
        _config.Client.DefaultRequestHeaders.Add("Host", "ranobes.net");
    }

    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);
        url = new Uri($"http://{HOST}/novels/{GetId(url)}.html");
        
        var doc = await GetSafety(url);

        var book = new Book(url.ReplaceHost(SystemUrl.Host)) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.QuerySelector("h1.title").FirstChild.InnerText.Trim().HtmlDecode(),
            Author = new Author("Ранобэс")
        };
            
        return book;
    }

    private async Task<Uri> GetMainUrl(Uri url) {
        if (GetId(url).StartsWith("read-", StringComparison.InvariantCultureIgnoreCase)) {
            var doc = await GetSafety(url);
            return new Uri(url, doc.QuerySelector("div.category a").Attributes["href"].Value);
        }

        return url;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();

        foreach (var ranobeChapter in await GetChapters(GetTocLink(doc, url))) {
            Console.WriteLine($"Загружаю главу {ranobeChapter.Title.CoverQuotes()}");
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
        var doc = await GetSafety(new Uri(mainUrl, url));
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
            
        return sb.AsHtmlDoc().RemoveNodes(t => t.Name is "script" or "br" || t.Id?.Contains("yandex_rtb") == true);
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.poster img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
    }

    private static Uri GetTocLink(HtmlDocument doc, Uri uri) {
        var relativeUri = doc.QuerySelector("div.r-fullstory-chapters-foot a[title~=contents]").Attributes["href"].Value;
        return new Uri(uri, relativeUri);
    }
    
    
    private static WindowData GetData(HtmlDocument doc) {
        var match = new Regex("window.__DATA__ = (?<data>{.*})</script>", RegexOptions.Compiled | RegexOptions.Singleline).Match(doc.Text).Groups["data"].Value;
        var windowData = match.Deserialize<WindowData>();
        return windowData;
    }
        
    private async Task<IEnumerable<UrlChapter>> GetChapters(Uri tocUri) {
        var doc = await GetSafety(tocUri);
        var data = GetData(doc);
            
        Console.WriteLine("Получаем оглавление");
        var chapters = new List<UrlChapter>();
        for (var i = 1; i <= data.Pages; i++) {
            var url = i == 1 ? tocUri : new Uri($"{tocUri.AbsoluteUri}page/{i}/");
            doc = await GetSafety(url);
            data = GetData(doc);
            var ranobesChapters = data
                .Chapters
                .Select(a => new UrlChapter(new Uri($"https://ranobes.net/read-{a.Id}.html"), a.Title))
                .ToList();
            
            chapters.AddRange(ranobesChapters);
        }
        
        Console.WriteLine($"Получено {chapters.Count} глав");

        chapters.Reverse();
        return chapters;
    }

    private async Task<HtmlDocument> GetSafety(Uri url) {
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
        while (doc.GetTextBySelector("h1.title.h2") == "Antibot system") {
            Console.WriteLine($"Обнаружена каптча. Перейдите по ссылке {url}, введите каптчу и нажмите Enter...");
            Console.Read();
            doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
        }

        return doc;
    }
}