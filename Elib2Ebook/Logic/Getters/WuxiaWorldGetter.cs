using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class WuxiaWorldGetter : GetterBase {
    public WuxiaWorldGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://wuxiaworld.ru/");
    
    private async Task<Uri> GetMainUrl(Uri url) {
        if (url.Segments[1] == "category/") {
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
            url = new Uri(doc.QuerySelector("h3[itemprop=name] a").Attributes["href"].Value);
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
        var catId = Regex.Match(doc.ParsedText, @"catID = (?<catId>\d+)").Groups["catId"].Value;
        var slug = url.Segments[1].Trim('/');
        var result = new List<UrlChapter>();
        
        foreach (var span in doc.QuerySelectorAll("ul.myUL span.caret[data-id]")) {
            var payload = new FormUrlEncodedContent(new Dictionary<string, string> {
                ["cat_id"] = catId,
                ["cat_slug"] = slug,
                ["chapter"] = span.Attributes["data-id"].Value
            });

            var data = await Config.Client.PostWithTriesAsync(new Uri("https://wuxiaworld.ru/wp-content/themes/Wuxia/template-parts/post/menu-query.php"), payload);
            var tocDoc = await data.Content.ReadAsStringAsync().ContinueWith(t => t.Result.AsHtmlDoc());

            result.AddRange(tocDoc.QuerySelectorAll("li a").Select(a => new UrlChapter(new Uri(url, a.Attributes["href"].Value), a.InnerText.HtmlDecode())));
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
        return doc.QuerySelector("div.entry-content").RemoveNodes("> :not(p)").InnerHtml.AsHtmlDoc();
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("meta[property=og:image]")?.Attributes["content"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, imagePath)) : Task.FromResult(default(Image));
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