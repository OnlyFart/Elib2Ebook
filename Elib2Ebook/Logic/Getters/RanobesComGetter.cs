using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class RanobesComGetter : GetterBase {
    public RanobesComGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ranobes.com");
    
    // cloudflare :(
    private const string IP = "5.252.195.125";

    protected override string GetId(Uri url) {
        return base.GetId(url).Split(".")[0];
    }

    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);
        url = new Uri($"https://{IP}/ranobe/{GetId(url)}.html");
        
        var doc = await GetHtmlDocument(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.QuerySelector("h1.title").FirstChild.InnerText.Trim().HtmlDecode(),
            Author = new Author(doc.GetTextBySelector("span[itemprop=creator]") ?? "Ranobes"),
            Annotation = doc.QuerySelector("div[itemprop=description]")?.RemoveNodes("style")?.InnerHtml
        };
            
        return book;
    }

    private async Task<Uri> GetMainUrl(Uri url) {
        if (url.Segments[1] == "chapters/" || !url.Segments.Last().EndsWith(".html")) {
            var doc = await GetHtmlDocument(new Uri(new Uri($"https://{IP}/"), url.AbsolutePath));
            return new Uri(url, doc.QuerySelector("h5 a").Attributes["href"].Value);
        }

        return url;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();

        foreach (var ranobeChapter in await GetToc(GetTocLink(doc, url))) {
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
        var doc = await GetHtmlDocument(new Uri(mainUrl, url));
        var sb = new StringBuilder();
        foreach (var node in doc.QuerySelectorAll("#arrticle > :not(.splitnewsnavigation)")) {
            var tag = node.Name == "#text" ? "p" : node.Name;
            if (tag == "img") {
                sb.Append(node.OuterHtml.Trim().CoverTag(tag));
            } else {
                if (node.InnerHtml?.Contains("window.yaContextCb") == false) {
                    sb.Append(node.InnerHtml.CoverTag(tag));
                }
            }
        }
            
        return sb.AsHtmlDoc().RemoveNodes(t => t.Name is "script" or "br" || t.Id?.Contains("yandex_rtb") == true);
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
        
        return new Uri(uri, new Uri(relativeUri).AbsolutePath.Trim('/'));
    }
        
    private async Task<IEnumerable<UrlChapter>> GetToc(Uri tocUri) {
        var doc = await GetHtmlDocument(tocUri);
        var lastA = doc.QuerySelector("div.pages a:last-child")?.InnerText;
        var pages = string.IsNullOrWhiteSpace(lastA) ? 1 : int.Parse(lastA);
            
        Console.WriteLine("Получаю оглавление");
        var chapters = new List<UrlChapter>();
        for (var i = 1; i <= pages; i++) {
            doc = await GetHtmlDocument(new Uri(tocUri.AbsoluteUri + "/page/" + i));
            var ranobesChapters = doc
                .QuerySelectorAll("#dle-content > .cat_block.cat_line a")
                .Select(a => new UrlChapter(new Uri(new Uri($"https://{IP}/"), new Uri(a.Attributes["href"].Value).AbsolutePath), a.Attributes["title"].Value))
                .ToList();
            
            chapters.AddRange(ranobesChapters);
        }
        
        Console.WriteLine($"Получено {chapters.Count} глав");

        chapters.Reverse();
        return SliceToc(chapters);
    }

    private HttpRequestMessage CreateRequestMessage(Uri uri) {
        var message = new HttpRequestMessage(HttpMethod.Get, uri);
        message.Headers.Add("Host", SystemUrl.Host);
        return message;
    }

    private async Task<HtmlDocument> GetHtmlDocument(Uri uri) {
        var response = await Config.Client.SendWithTriesAsync(() => CreateRequestMessage(uri));
        var content = await response.Content.ReadAsStringAsync();
            
        return content.AsHtmlDoc();
    }

    protected override HttpRequestMessage GetImageRequestMessage(Uri uri) {
        if (uri.Host != SystemUrl.Host && uri.Host != IP) {
            return base.GetImageRequestMessage(uri);
        }
        
        var message = new HttpRequestMessage(HttpMethod.Get, uri.ReplaceHost(IP));
        message.Headers.Add("Host", SystemUrl.Host);
        return message;
    }
}