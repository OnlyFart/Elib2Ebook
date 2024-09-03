using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using Core.Types.WuxiaWorld;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Core.Logic.Getters; 

public class WuxiaWorldGetter : GetterBase {
    public WuxiaWorldGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://wuxiaworld.ru/");
    
    private async Task<Uri> GetMainUrl(Uri url) {
        if (url.GetSegment(1) == "category") {
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
            url = doc.QuerySelector("h2[itemprop=name] a").Attributes["href"].Value.AsUri();
        }

        return url;
    }
    
    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);
        var doc = await GetSafety(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1"),
            Author = new Author("WuxiaWorld"),
        };
            
        return book;
    }

    private async Task<IEnumerable<UrlChapter>> GetToc(HtmlDocument doc, Uri url) {
        var catId = Regex.Match(doc.ParsedText, "data-cat=\"(?<catId>\\d+)\"").Groups["catId"].Value;
        var result = new List<UrlChapter>();
        
        foreach (var span in doc.QuerySelectorAll("ul.myUL span.caret[data-id]")) {
            var offset = (int.Parse(span.Attributes["data-id"].Value) - 1) * 100;
            var payload = new FormUrlEncodedContent(new Dictionary<string, string> {
                ["cat_id"] = catId,
                ["offset"] = offset.ToString()
            });

            var post = await Config.Client.PostWithTriesAsync(SystemUrl.MakeRelativeUri("/wp-content/themes/Wuxia/template-parts/post/menu-query.php"), payload);
            var toc = await post.Content.ReadFromJsonAsync<WuxiaWorldToc[]>();
            result.AddRange(toc.Select((a, i) => new UrlChapter(url.MakeRelativeUri(a.PostName), $"Глава {offset + i + 1}")));
        }

        return SliceToc(result);
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();

        foreach (var bookChapter in await GetToc(doc, url)) {
            var chapter = new Chapter();
            Console.WriteLine($"Загружаю главу {bookChapter.Title.CoverQuotes()}");
            
            var chapterDoc = await GetChapter(bookChapter.Url);
            chapter.Title = bookChapter.Title;
            chapter.Images = await GetImages(chapterDoc, url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri url) {
        var doc = await GetSafety(url);
        return doc.QuerySelector("div.js-full-content").RemoveNodes("> :not(p)").InnerHtml.AsHtmlDoc();
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("meta[property=og:image]")?.Attributes["content"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
    
    private async Task<HtmlDocument> GetSafety(Uri url) {
        var response = await Config.Client.GetWithTriesAsync(url);
        await Task.Delay(TimeSpan.FromSeconds(1));
        
        while (response == default || response.StatusCode == HttpStatusCode.ServiceUnavailable) {
            Console.WriteLine("Получен бан от системы. Жду...");
            var errorTimeout = TimeSpan.FromSeconds(30);
            response = await Config.Client.GetWithTriesAsync(url, errorTimeout);
            await Task.Delay(errorTimeout);
        }
        
        return await response.Content.ReadAsStringAsync().ContinueWith(t => t.Result.AsHtmlDoc());
    }
}