using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class LibboxGetter : GetterBase {
    public LibboxGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://libbox.ru/");
    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = SystemUrl.MakeRelativeUri($"/book/{bookId}");
        
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var title = doc.GetTextBySelector("h1.product__title");
        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(bookId, title),
            Title = title,
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div.woocommerce-Tabs-panel--description div.container")?.InnerHtml,
            Seria = GetSeria(doc, url)
        };

        return book; 
    }
    
    private static Seria GetSeria(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("div.product__meta a[href*=/book-series/]");
        if (string.IsNullOrWhiteSpace(a.GetText())) {
            return default;
        }

        return new Seria {
            Name = a.GetText(),
            Url = url.MakeRelativeUri(a.Attributes["href"].Value)
        };
    }

    private async Task AddChapter(ICollection<Chapter> chapters, Chapter chapter, StringBuilder text) {
        if (chapter == null) {
            return;
        }
        
        var chapterDoc = text.AsHtmlDoc();
        chapter.Images = await GetImages(chapterDoc, SystemUrl);
        chapter.Content = chapterDoc.DocumentNode.InnerHtml;
        chapters.Add(chapter);
    }

    private static bool IsSingleChapter(IEnumerable<HtmlNode> nodes) {
        var firstNode = nodes.First();
        return firstNode.Name != "h2" || string.IsNullOrWhiteSpace(firstNode.InnerText);
    }

    private static Chapter CreateChapter(string title) {
        return new Chapter {
            Title = title
        };
    }

    private async Task<int> GetPages(string bookId) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri($"/books/{bookId}"));
        return doc
            .QuerySelectorAll("ul.pagination a.page-numbers")
            .Where(a => int.TryParse(a.GetText(), out var _))
            .Select(a => int.Parse(a.GetText()))
            .Max();
    }

    private async Task<IEnumerable<Chapter>> FillChapters(string bookId, string title) {
        var chapters = new List<Chapter>();
        Chapter chapter = null;
        var singleChapter = true;
        var text = new StringBuilder();
        var pages = await GetPages(bookId);
        
        for (var i = 1; i <= pages; i++) {
            Console.WriteLine($"Получаю страницу {i}/{pages}");
            var page = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri($"/books/{bookId}?page_book={i}"));;

            var content = page.QuerySelector("div.entry-content");
            var nodes = content.QuerySelectorAll("> h2, > p, > img");
            nodes = nodes.Count == 0 ? content.RemoveNodes("script, ins").ChildNodes : nodes;
            singleChapter = i == 1 ? IsSingleChapter(nodes) : singleChapter;

            foreach (var node in nodes) {
                if (singleChapter || node.Name != "h2") {
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
                    chapter = CreateChapter(node.InnerHtml.HtmlDecode());
                }
            }
        }

        await AddChapter(chapters, chapter ?? CreateChapter(title), text);
        return chapters;
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var author = doc.QuerySelector("div.author a");
        return new Author(author.InnerText.HtmlDecode(), url.MakeRelativeUri(author.Attributes["href"]?.Value ?? string.Empty));
    }
    
    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.book-images img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}