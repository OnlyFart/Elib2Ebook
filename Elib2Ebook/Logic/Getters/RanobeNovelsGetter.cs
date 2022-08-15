using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.RanobeNovels;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class RanobeNovelsGetter : GetterBase {
    public RanobeNovelsGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ranobe-novels.ru/");

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = new Uri($"https://ranobe-novels.ru/ranobe/{bookId}/");
        var doc = await GetSafety(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1"),
            Author = new Author("ranobe-novels")
        };
            
        return book;
    }
    
    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();

        var toc = await GetToc(doc);
        if (toc.Count == 0) {
            doc = await Config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://ranobe-novels.ru/{GetId(url)}"));
            toc = await GetToc(doc);
        }
        
        foreach (var ranobeChapter in toc) {
            Console.WriteLine($"Загружаю главу {ranobeChapter.Title.CoverQuotes()}");
            var chapter = new Chapter();
            var chapterDoc = await GetChapter(new Uri($"https://ranobe-novels.ru/{ranobeChapter.Name}"));
            chapter.Images = await GetImages(chapterDoc, url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = ranobeChapter.Title;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri url) {
        var doc = await GetSafety(url);
        return doc.QuerySelectorAll("div.entry-content > p")
            .Aggregate(new StringBuilder(), (sb, node) => sb.Append(node.InnerHtml.CoverTag("p")))
            .AsHtmlDoc();
    }

    private async Task<List<RanobeNovelsChapter>> GetToc(HtmlDocument doc) {
        var catId = Regex.Match(doc.ParsedText, @"let cat_id = (?<id>\d+);").Groups["id"].Value;
        var security = Regex.Match(doc.ParsedText, "\"nonce\":\"(?<id>.*?)\"").Groups["id"].Value;
        var data = new Dictionary<string, string> {
            ["action"] = "select_Ajax",
            ["query"] = "catChapters",
            ["cat_id"] = catId,
            ["security"] = security
        };

       
        var response = await Config.Client.PostAsync(new Uri("https://ranobe-novels.ru/wp-admin/admin-ajax.php"), new FormUrlEncodedContent(data));
        if (response.StatusCode != HttpStatusCode.OK) {
            return new List<RanobeNovelsChapter>();
        }
        
        var result = await response.Content.ReadAsStringAsync().ContinueWith(t => t.Result.Deserialize<List<RanobeNovelsChapter>>());
        result.Reverse();

        return result;
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