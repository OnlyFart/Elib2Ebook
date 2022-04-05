using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using OnlineLib2Ebook.Configs;
using OnlineLib2Ebook.Extensions;
using OnlineLib2Ebook.Types.Book;

namespace OnlineLib2Ebook.Logic.Getters; 

public class ReadliGetter : GetterBase {
    public ReadliGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://readli.net");
    public override async Task<Book> Get(Uri url) {
        Init();
        
        url = await GetMainUrl(url);

        var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
        var pages = long.Parse(doc.GetTextBySelector("span.button-pages__right").Split(' ')[0]);
        var imageDiv = doc.QuerySelector("div.book-image");
        var href = new Uri(url, imageDiv.QuerySelector("a").Attributes["href"].Value);
        var bookId = GetBookId(href);
            
        var name = doc.GetTextBySelector("h1.main-info__title");
        var author = doc.GetTextBySelector("a.main-info__link");
            
        var book = new Book {
            Cover = await GetCover(imageDiv, url),
            Chapters = await FillChapters(bookId, pages, name),
            Title = name,
            Author = author
        };
            
        return book; 
    }

    private static long GetBookId(Uri uri) {
        return long.Parse(uri.Query.Trim('?').Split("&").FirstOrDefault(p => p.StartsWith("b=")).Replace("b=", ""));
    }

    private async Task<Uri> GetMainUrl(Uri url) {
        if (GetId(url).StartsWith("chitat-online", StringComparison.InvariantCultureIgnoreCase)) {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://readli.net/chitat-online/?b={GetBookId(url)}"));
            var href = doc.QuerySelector("h1 a").Attributes["href"].Value;
            return new Uri(url, href);
        }

        return url;
    }

    private Task<Image> GetCover(HtmlNode doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
    }

    private async Task AddChapter(ICollection<Chapter> chapters, Chapter chapter, StringBuilder text) {
        if (chapter == null) {
            return;
        }
        
        var chapterDoc = text.ToString().HtmlDecode().AsHtmlDoc();
        chapter.Images = await GetImages(chapterDoc, new Uri("https://readli.net/chitat-online/"));
        chapter.Content = chapterDoc.DocumentNode.InnerHtml;
        chapters.Add(chapter);
    }

    private static bool IsSingleChapter(IEnumerable<HtmlNode> nodes) {
        var firstNode = nodes.First();
        return firstNode.Name != "h3" || string.IsNullOrWhiteSpace(firstNode.InnerText);
    }

    private static Chapter CreateChapter(string title) {
        return new Chapter {
            Title = title
        };
    }

    private async Task<List<Chapter>> FillChapters(long bookId, long pages, string name) {
        var chapters = new List<Chapter>();
        Chapter chapter = null;
        var singleChapter = true;
        var text = new StringBuilder();
            
        for (var i = 1; i <= pages; i++) {
            Console.WriteLine($"Получаю страницу {i}/{pages}");
            var page = await _config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://readli.net/chitat-online/?b={bookId}&pg={i}"));
            var content = page.QuerySelector("div.reading__text");
            var nodes = content.QuerySelectorAll("> h3, > p");
            singleChapter = i == 1 ? IsSingleChapter(nodes) : singleChapter;

            foreach (var node in nodes) {
                if (singleChapter || node.Name != "h3") {
                    text.Append($"<p>{node.InnerText.HtmlEncode()}</p>");
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