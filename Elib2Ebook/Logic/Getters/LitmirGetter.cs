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

public class LitmirGetter : GetterBase {
    public LitmirGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://litmir.me/");

    protected override string GetId(Uri url) => url.GetQueryParameter("b");

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = SystemUrl.MakeRelativeUri($"/bd/?b={bookId}");

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        var pages = long.Parse(doc.GetTextBySelector("span[itemprop=numberOfPages]"));

        var name = doc.GetTextBySelector("div[itemprop=name]");

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(bookId, pages, name),
            Title = name,
            Author = new Author(doc.QuerySelector("span[itemprop=author] meta")?.Attributes["content"]?.Value ?? "Litmir"),
            Annotation = doc.QuerySelector("div[itemprop=description]")?.InnerHtml,
            Seria = GetSeria(doc, url)
        };
            
        return book; 
    }
    
    private static Seria GetSeria(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("td.bd_desc2 a[href^=/books_in_series/]");
        if (a != default) {
            var text = a.GetText();
            if (!string.IsNullOrWhiteSpace(text)) {
                var seria = new Seria {
                    Url = url.MakeRelativeUri(a.Attributes["href"].Value)
                };
                
                if (text.Contains('#')) {
                    var split = text.Split("#");
                    if (int.TryParse(split.Last(), out var number)) {
                        seria.Number = number.ToString();
                        seria.Name = split[0].Trim();
                    } else {
                        seria.Name = text;
                    }
                } else {
                    seria.Name = text;
                }
                
                return seria;
            }
            
            
        }

        return default;
    }
    
    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("img[jq=BookCover]")?.Attributes["src"]?.Value;
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
        return node.Name == "a" && node.Attributes["name"]?.Value?.StartsWith("section") == true;
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

    private async Task<HtmlDocument> GetChapter(string bookId, int page) {
        var response = await Config.Client.GetWithTriesAsync(SystemUrl.MakeRelativeUri($"/br/?b={bookId}&p={page}"));
        if (response == default) {
            return default;
        }

        var doc = await response.Content.ReadAsStringAsync().ContinueWith(t => t.Result.AsHtmlDoc());
        foreach (var node in doc.QuerySelectorAll("p")) {
            node.Attributes.RemoveAll();
        }
        
        return doc;
    }

    private async Task<List<Chapter>> FillChapters(string bookId, long pages, string name) {
        var chapters = new List<Chapter>();
        Chapter chapter = null;
        var singleChapter = true;
        var text = new StringBuilder();
            
        for (var i = 1; i <= pages; i++) {
            Console.WriteLine($"Получаю страницу {i}/{pages}");
            var page = await GetChapter(bookId, i);

            var nodes = page.QuerySelector("div.page_text").RemoveNodes("div[id^=adrun], script").ChildNodes;
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