using System;
using System.Collections.Generic;
using System.Linq;
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

public class RulateGetter : GetterBase {
    public RulateGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://tl.rulate.ru");

    protected override string GetId(Uri url) {
        var segments = url.Segments;
        return (segments.Length == 3 ? base.GetId(url) : segments[2]).Trim('/');
    }

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = new Uri($"https://tl.rulate.ru/book/{bookId}");
        await Mature(url);
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url, bookId),
            Title = GetTitle(doc),
            Author = GetAuthor(doc)
        };
            
        return book;
    }

    private static string GetTitle(HtmlDocument doc) {
        var match = Regex.Match(doc.ParsedText, "t_title: '(?<title>.*?)',");
        return match.Success ? match.Groups["title"].Value : doc.GetTextBySelector("h1");
    }

    private static Author GetAuthor(HtmlDocument doc) {
        var def = new Author("rulate");
        foreach (var p in doc.QuerySelectorAll("#Info p")) {
            var strong = p.QuerySelector("strong");
            if (strong != null && strong.InnerText.Contains("Автор")) {
                var author = p.GetTextBySelector("em");
                return author == null ? def : new Author(author);
            }
        }

        return def;
    }
        
    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.slick img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
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
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://tl.rulate.ru/book/{bookId}/{chapterId}/ready"));
        return (doc.GetTextBySelector("h1") == "Доступ запрещен" ? string.Empty : doc.QuerySelector("div.content-text")?.InnerHtml ?? string.Empty).AsHtmlDoc();
    }

    private IEnumerable<IdChapter> GetToc(HtmlDocument doc) {
        var result = doc.QuerySelectorAll("#Chapters tr[data-id]")
            .Select(chapter => new IdChapter(chapter.Attributes["data-id"].Value, chapter.GetTextBySelector("td.t")));
        
        return SliceToc(result);
    }

    private async Task Mature(Uri url) {
        var data = new Dictionary<string, string> {
            { "path", url.LocalPath },
            { "ok", "Да" }
        };

        await Config.Client.PostAsync(new Uri($"https://tl.rulate.ru/mature?path={url.LocalPath}"), new FormUrlEncodedContent(data));
    }
}