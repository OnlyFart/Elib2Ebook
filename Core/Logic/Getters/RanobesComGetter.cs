using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using Core.Types.Ranobes;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Core.Logic.Getters; 

public class RanobesComGetter : GetterBase {
    public RanobesComGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ranobes.com/");

    protected override string GetId(Uri url) => base.GetId(url).Split(".")[0];

    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);
        url = SystemUrl.MakeRelativeUri($"/ranobe/{GetId(url)}.html");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        await Antibot(doc, url);
        doc = await Config.Client.GetHtmlDocWithTriesAsync(url); 

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.QuerySelector("h1.title").FirstChild.InnerText.Trim().HtmlDecode(),
            Author = new Author(doc.GetTextBySelector("span[itemprop=creator]") ?? "Ranobes"),
            Annotation = doc.QuerySelector("div[itemprop=description]")?.RemoveNodes("style")?.InnerHtml
        };
            
        return book;
    }

    private async Task Antibot(HtmlDocument doc, Uri referrer) {
        var h1 = Regex.Match(doc.ParsedText, "var h1 = \'(?<id>.*?)\'").Groups["id"].Value;
        var h2 = Regex.Match(doc.ParsedText, "var h2 = \'(?<id>.*?)\'").Groups["id"].Value;
        var date = Regex.Match(doc.ParsedText, "var date = \'(?<id>.*?)\'").Groups["id"].Value;
        var cid = Regex.Match(doc.ParsedText, "var cid = \'(?<id>.*?)\'").Groups["id"].Value;
        var ip = Regex.Match(doc.ParsedText, "var ip = \'(?<id>.*?)\'").Groups["id"].Value;
        var ptr = Regex.Match(doc.ParsedText, "var ptr = \'(?<id>.*?)\'").Groups["id"].Value;
        var v = Regex.Match(doc.ParsedText, "var v = \'(?<id>.*?)\'").Groups["id"].Value;
        var antibot = Regex.Match(doc.ParsedText, "antibot_(?<id>.*?)=").Groups["id"].Value;

        var data = new Dictionary<string, string> {
            { "hdc", "0" },
            { "scheme", "https" },
            { "a", "0" },
            { "date", date },
            { "country", "RU" },
            { "h1", h1 },
            { "h2", h2 },
            { "ip", ip },
            { "v", v },
            { "cid", cid },
            { "ptr", ptr },
            { "w", "2560" },
            { "h", "1440" },
            { "cw", "2560" },
            { "ch", "770" },
            { "co", "24" },
            { "pi", "24" },
            { "ref", "ranobes.com" },
            { "xxx", string.Empty },
        };
        
        Config.Client.DefaultRequestHeaders.Add("Referer", referrer.ToString());
        var post = await Config.Client.PostAsync(SystemUrl.MakeRelativeUri("antibot8/ab.php"), new FormUrlEncodedContent(data));
        var cookie = await post.Content.ReadFromJsonAsync<RanobesCookie>();
        Config.CookieContainer.Add(SystemUrl, new Cookie($"antibot_{antibot}", cookie.Cookie + "-" + date));
        Config.Client.DefaultRequestHeaders.Remove("Referer");
    }

    private async Task<Uri> GetMainUrl(Uri url) {
        if (url.GetSegment(1) == "chapters" || !url.Segments.Last().EndsWith(".html")) {
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri(url.AbsolutePath));
            return url.MakeRelativeUri(doc.QuerySelector("a[rel=up]").Attributes["href"].Value);
        }

        return url;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();

        foreach (var ranobeChapter in await GetToc(GetTocLink(doc, url))) {
            Console.WriteLine($"Загружаю главу {ranobeChapter.Title.CoverQuotes()}");
            var chapter = new Chapter();
            var chapterDoc = await GetChapter(ranobeChapter);
            chapter.Images = await GetImages(chapterDoc, url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = ranobeChapter.Title;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(UrlChapter chapter) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(chapter.Url);
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
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }

    private Uri GetTocLink(HtmlDocument doc, Uri uri) {
        var relativeUri = doc.QuerySelector("div.r-fullstory-btns a[title~=оглавление]").Attributes["href"].Value.HtmlDecode();
        if (!relativeUri.Contains("chapters")) {
            relativeUri = $"/chapters/{string.Join("-", GetId(uri).Split(".")[0].Split("-").Skip(1))}";
        }
        
        return uri.MakeRelativeUri(relativeUri.AsUri().AbsolutePath.Trim('/'));
    }
        
    private async Task<IEnumerable<UrlChapter>> GetToc(Uri tocUri) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(tocUri);
        var lastA = doc.QuerySelector("div.pages a:last-child")?.InnerText;
        var pages = string.IsNullOrWhiteSpace(lastA) ? 1 : int.Parse(lastA);
            
        Console.WriteLine("Получаю оглавление");
        var result = new List<UrlChapter>();
        for (var i = 1; i <= pages; i++) {
            doc = await Config.Client.GetHtmlDocWithTriesAsync(tocUri.AppendSegment($"/page/{i}"));
            var chapters = doc
                .QuerySelectorAll("#dle-content > .cat_block.cat_line a[title]")
                .Select(a => new UrlChapter(a.Attributes["href"].Value.AsUri(), string.IsNullOrWhiteSpace(a.Attributes["title"].Value) ? "Без названия" : a.Attributes["title"].Value))
                .ToList();
            
            result.AddRange(chapters);
        }
        
        Console.WriteLine($"Получено {result.Count} глав");

        result.Reverse();
        return SliceToc(result);
    }
}