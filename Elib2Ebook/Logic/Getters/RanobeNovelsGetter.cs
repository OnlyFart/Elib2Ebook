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
        var uri = new Uri($"https://ranobe-novels.ru/ranobe/{bookId}/");
        var doc = await GetSafety(url);

        var book = new Book(uri) {
            Cover = await GetCover(doc, uri),
            Chapters = await FillChapters(doc, uri),
            Title = doc.GetTextBySelector("h1"),
            Author = new Author("ranobe-novels")
        };
            
        return book;
    }
    
    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();
            
        foreach (var ranobeChapter in await GetChapters(doc)) {
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
            .Aggregate(new StringBuilder(), (sb, node) => sb.Append($"<p>{node.InnerHtml}</p>"))
            .AsHtmlDoc();
    }

    private async Task<IEnumerable<RanobeNovelsChapter>> GetChapters(HtmlDocument doc) {
        var data = new Dictionary<string, string> {
            ["action"] = "select_Ajax",
            ["query"] = "catChapters",
            ["cat_id"] = GetId(new Uri(doc.QuerySelector("link[rel=alternate][type=application/json]").Attributes["href"].Value)),
            ["security"] = Regex.Match(doc.ParsedText, "\"nonce\":\"(?<id>.*?)\"").Groups["id"].Value
        };

        var response = await _config.Client.PostAsync(new Uri("https://ranobe-novels.ru/wp-admin/admin-ajax.php"), new FormUrlEncodedContent(data));
        var result = await response.Content.ReadAsStringAsync().ContinueWith(t => t.Result.Deserialize<List<RanobeNovelsChapter>>());
        result.Reverse();
        return result;
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("meta[property=og:image]")?.Attributes["content"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, imagePath)) : Task.FromResult(default(Image));
    }

    private async Task<HtmlDocument> GetSafety(Uri url) {
        var response = await _config.Client.GetWithTriesAsync(url);
        await Task.Delay(TimeSpan.FromSeconds(1));
        
        while (response == default || response.StatusCode == HttpStatusCode.ServiceUnavailable) {
            Console.WriteLine("Получили бан от системы. Жду...");
            var errorTimeout = TimeSpan.FromSeconds(30);
            response = await _config.Client.GetWithTriesAsync(url, errorTimeout);
            await Task.Delay(errorTimeout);
        }
        
        return await response.Content.ReadAsStringAsync().ContinueWith(t => t.Result.AsHtmlDoc());
    }
}