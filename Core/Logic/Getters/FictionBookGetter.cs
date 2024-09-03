using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class FictionBookGetter : GetterBase{
    public FictionBookGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://fictionbook.ru");
    
    private Uri GetMainUrl(Uri url) {
        if (url.Segments.Last().StartsWith("read_online.html")) {
            url = SystemUrl.MakeRelativeUri(string.Join(string.Empty, url.Segments[..^1]));
        }

        return url;
    }

    public override async Task<Book> Get(Uri url) {
        url = GetMainUrl(url);

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var name = doc.GetTextBySelector("h1 span[itemprop=name]");

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(url, name),
            Title = name,
            Author = GetAuthor(doc),
            Annotation = doc.QuerySelector("div.biblio_book__annotation")?.InnerHtml,
        };
            
        return book; 
    }

    private Author GetAuthor(HtmlDocument doc) {
        var a = doc.QuerySelector("div.info a[data-widget-litres-author][href]");
        return a == default ? 
            new Author("FictionBook") : 
            new Author(a.GetText(), SystemUrl.MakeRelativeUri(a.Attributes["href"].Value));
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.cover_float img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }

    private async Task AddChapter(ICollection<Chapter> chapters, Chapter chapter, StringBuilder text) {
        if (chapter == null) {
            return;
        }
        
        var chapterDoc = text.AsHtmlDoc();
        chapter.Images = await GetImages(chapterDoc, SystemUrl.MakeRelativeUri("/br/"));
        chapter.Content = chapterDoc.DocumentNode.InnerHtml;
        chapters.Add(chapter);
    }

    private static bool IsChapterHeader(HtmlNode node) {
        return node.Name == "h2";
    }

    private static bool IsSingleChapter(IEnumerable<HtmlNode> nodes) {
        var firstNode = nodes.First();
        return !IsChapterHeader(firstNode) || string.IsNullOrWhiteSpace(firstNode.InnerText);
    }

    private static Chapter CreateChapter(string title) {
        return new Chapter {
            Title = title
        };
    }

    private Task<HtmlDocument> GetChapter(Uri url, int page) {
        return Config.Client.GetHtmlDocWithTriesAsync(url.AppendSegment("read_online.html").AppendQueryParameter("page", page));
    }

    private async Task<int> GetPages(Uri url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url.AppendSegment("read_online.html"));
        var pages = doc.QuerySelectorAll("div.reader_pager_bottom > a[href]")
            .Where(a => int.TryParse(a.GetText(), out _))
            .Select(a => int.Parse(a.GetText()))
            .ToList();
        
        return pages.Any() ? pages.Max() : 1;
    }

    private async Task<List<Chapter>> FillChapters(Uri url, string name) {
        var chapters = new List<Chapter>();
        Chapter chapter = null;
        var singleChapter = true;
        var text = new StringBuilder();
        var pages = await GetPages(url);
            
        for (var i = 1; i <= pages; i++) {
            Config.Logger.LogInformation($"Получаю страницу {i}/{pages}");
            var page = await GetChapter(url, i);

            var nodes  = page.QuerySelector("#onlineread").RemoveNodes("div.biblio_book__wrap").ChildNodes;
            singleChapter = i == 1 ? IsSingleChapter(nodes) : singleChapter;

            foreach (var node in nodes) {
                if (singleChapter || !IsChapterHeader(node)) {
                    if (node.Name == "img" && node.Attributes["src"] != null) {
                        text.Append($"<img src='{node.Attributes["src"].Value}'/>");
                    } else {
                        if (!string.IsNullOrWhiteSpace(node.InnerHtml)) {
                            text.Append(node.InnerHtml.HtmlDecode().CoverTag("p"));
                        }
                    }
                } else {
                    await AddChapter(chapters, chapter, text);
                    text.Clear();
                    chapter = CreateChapter(node.InnerText.HtmlDecode());
                }
            }
        }

        await AddChapter(chapters, chapter ?? CreateChapter(name), text);
        return chapters;
    }
}