using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Elib2Ebook.Extensions;

namespace Elib2Ebook.Logic.Getters; 

public class BiglibaGetter : GetterBase{
    public BiglibaGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://bigliba.com/");
    
    protected override string GetId(Uri url) {
        return url.Segments[2].Trim('/');
    }
    
    public override async Task<Book> Get(Uri url) {
        var token = await GetToken();
        var bookId = GetId(url);
        var uri = new Uri($"https://bigliba.com/books/{bookId}");
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(uri);
        var title = doc.GetTextBySelector("h1[itemprop=name]");

        var book = new Book {
            Cover = await GetCover(doc, uri),
            Chapters = await FillChapters(uri, bookId, token, title),
            Title = doc.GetTextBySelector("h1[itemprop=name]"),
            Author = doc.GetTextBySelector("h2[itemprop=author]")
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(Uri uri, string bookId, string token, string title) {
        var result = new List<Chapter>();
            
        foreach (var id in await GetChapterIds(bookId)) {
            var chapter = new Chapter();
            var content = await GetChapter(bookId, id, token);
            if (content.StartsWith("{\"status\":\"error\"")) {
                Console.WriteLine($"Часть {id} заблокирована");
                continue;
            }

            var doc = content.AsHtmlDoc();
            chapter.Title = Normalize(doc.GetTextBySelector("h1.capter-title") ?? title);

            doc.RemoveNodes(node => node.Name == "h1");
            chapter.Images = await GetImages(doc, uri);
            chapter.Content = doc.DocumentNode.InnerHtml;
            
            
            Console.WriteLine($"Загружаем главу {chapter.Title.CoverQuotes()}");

            result.Add(chapter);
        }

        return result;
    }
    
    private static string Normalize(string str) {
        return Regex.Replace(Regex.Replace(str, "\t|\n", " "), "\\s+", " ").Trim();
    }

    private async Task<string> GetChapter(string bookId, string id, string token) {
        var data = await _config.Client.PostWithTriesAsync(new Uri($"https://bigliba.com/reader/{bookId}/chapter"), GetData(id, token));
        return await data.Content.ReadAsStringAsync();
    }
    
    private static FormUrlEncodedContent GetData(string chapterId, string token) {
        var data = new Dictionary<string, string> {
            ["chapter"] = chapterId,
            ["_token"] = token,
        };

        return new FormUrlEncodedContent(data);
    }

    private async Task<IEnumerable<string>> GetChapterIds(string bookId) {
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://bigliba.com/reader/{bookId}"));
        return new Regex("capters: \\[(?<chapters>.*?)\\]").Match(doc.Text).Groups["chapters"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim('\"'));
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("img[itemprop=image]")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, imagePath)) : Task.FromResult(default(Image));
    }

    private async Task<string> GetToken() {
        return await _config.Client.GetHtmlDocWithTriesAsync(new Uri("https://bigliba.com/"))
            .ContinueWith(t => t.Result.QuerySelector("meta[name=_token]").Attributes["content"].Value);
    }
}