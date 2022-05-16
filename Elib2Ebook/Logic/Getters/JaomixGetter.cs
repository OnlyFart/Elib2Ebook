using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Jaomix;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Elib2Ebook.Extensions;

namespace Elib2Ebook.Logic.Getters; 

public class JaomixGetter : GetterBase {
    public JaomixGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://jaomix.ru/");
    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);
        var bookId = GetId(url);
        var uri = new Uri($"https://jaomix.ru/category/{bookId}/");
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(uri);

        var book = new Book(uri) {
            Cover = await GetCover(doc, uri),
            Chapters = await FillChapters(doc, uri),
            Title = doc.GetTextBySelector("h1"),
            Author = new Author("Jaomix")
        };
            
        return book;
    }

    private async Task<Uri> GetMainUrl(Uri url) {
        if (url.Segments[1] != "category/") {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
            return new Uri(url, doc.QuerySelector("span.entry-category a").Attributes["href"].Value);
        }

        return url;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();

        foreach (var jaomixChapter in await GetChapters(doc, url)) {
            Console.WriteLine($"Загружаю главу {jaomixChapter.Title.CoverQuotes()}");
            var chapter = new Chapter();
            var chapterDoc = await GetChapter(jaomixChapter.Url);
            chapter.Images = await GetImages(chapterDoc, url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = jaomixChapter.Title;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri jaomixChapterUrl) {
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(jaomixChapterUrl);
        var sb = new StringBuilder();
            
        foreach (var node in doc.QuerySelector("div.themeform").ChildNodes) {
            if (node.Name != "br" && node.Name != "script" && !string.IsNullOrWhiteSpace(node.InnerHtml) && node.Attributes["class"]?.Value?.Contains("adblock-service") == null) {
                var tag = node.Name == "#text" ? "p" : node.Name;
                sb.Append($"<{tag}>{node.InnerHtml.Trim()}</{tag}>");
            }
        }
            
        return sb.AsHtmlDoc();
    }

    private async Task<IEnumerable<JaomixChapter>> GetChapters(HtmlDocument doc, Uri url) {
        var termId = doc.QuerySelector("div.like-but").Id;

        var data = new Dictionary<string, string> {
            { "action", "toc" },
            { "selectall", termId }
        };
            
        var chapters = new List<JaomixChapter>();
        chapters.AddRange(ParseChapters(doc, url));
        
        doc = await _config.Client.PostHtmlDocWithTriesAsync(new Uri("https://jaomix.ru/wp-admin/admin-ajax.php"), new FormUrlEncodedContent(data));

        Console.WriteLine("Получаем оглавление");
            
        foreach (var option in doc.QuerySelector("select.sel-toc").ChildNodes) {
            var pageId = option.Attributes["value"].Value;
            if (pageId == "0") {
                continue;
            }
                
            data = new Dictionary<string, string> {
                { "action", "toc" },
                { "page", pageId },
                { "termid", termId }
            };

            var chapterDoc = await _config.Client.PostHtmlDocWithTriesAsync(new Uri("https://jaomix.ru/wp-admin/admin-ajax.php"), new FormUrlEncodedContent(data));
            chapters.AddRange(ParseChapters(chapterDoc, url));
        }
        
        Console.WriteLine($"Получено {chapters.Count} глав");

        chapters.Reverse();
        return chapters;
    }
        
    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.img-book img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
    }

    private static IEnumerable<JaomixChapter> ParseChapters(HtmlDocument doc, Uri url) {
        return doc.QuerySelectorAll("div.hiddenstab a").Select(a => new JaomixChapter(a.InnerText.Trim(), new Uri(url, a.Attributes["href"].Value)));
    }
}