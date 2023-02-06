using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class JaomixGetter : GetterBase {
    public JaomixGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://jaomix.ru/");
    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri($"/{GetId(url)}/");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1"),
            Author = new Author("Jaomix")
        };
            
        return book;
    }


    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();

        foreach (var jaomixChapter in GetToc(doc, url)) {
            Console.WriteLine($"Загружаю главу {jaomixChapter.Title.CoverQuotes()}");
            var chapter = new Chapter();
            var chapterDoc = await GetChapter(jaomixChapter.Url);
            chapter.Images = await GetImages(chapterDoc, url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = jaomixChapter.Title;

            result.Add(chapter);
            await Task.Delay(500);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri jaomixChapterUrl) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(jaomixChapterUrl);
        var sb = new StringBuilder();
            
        foreach (var node in doc.QuerySelector("div.themeform").ChildNodes) {
            if (node.Name != "br" && node.Name != "script" && !string.IsNullOrWhiteSpace(node.InnerHtml) && node.Attributes["class"]?.Value?.Contains("adblock-service") == null) {
                var tag = node.Name == "#text" ? "p" : node.Name;
                sb.Append(node.InnerHtml.HtmlDecode().CoverTag(tag));
            }
        }
            
        return sb.AsHtmlDoc();
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        var chapters = ParseChapters(doc, url).ToList();
        Console.WriteLine($"Получено {chapters.Count} глав");
        
        return SliceToc(chapters);
    }
        
    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.img-book img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }

    private static IEnumerable<UrlChapter> ParseChapters(HtmlDocument doc, Uri url) {
        return doc.QuerySelectorAll("form.download-chapter .hiddenstab .flex-dow-txt a").Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.InnerText.Trim())).Reverse();
    }
}