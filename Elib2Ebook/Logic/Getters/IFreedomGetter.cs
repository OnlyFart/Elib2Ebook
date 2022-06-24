using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class FreedomGetter : GetterBase{
    public FreedomGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ifreedom.su/");
    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);
        url = new Uri($"https://ifreedom.su/ranobe/{GetId(url)}/");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        
        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1.entry-title"),
            Author = GetAuthor()
        };
            
        return book;
    }

    private async Task<Uri> GetMainUrl(Uri url) {
        if (url.Segments[1].Trim('/') == "ranobe") {
            return url;
        }

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        return new Uri(url, doc.QuerySelector("div.bun2 a").Attributes["href"].Value);
    }

    private static Task<IEnumerable<UrlChapter>> GetToc(HtmlDocument doc, Uri url) {
        return Task.FromResult(doc
            .QuerySelectorAll("div.li-col1-ranobe a")
            .Select(a => new UrlChapter(new Uri(url, a.Attributes["href"].Value), a.GetText()))
            .Reverse());
    }
    
    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri uri) {
        var result = new List<Chapter>();

        foreach (var urlChapter in await GetToc(doc, uri)) {
            var chapter = new Chapter();
            Console.WriteLine($"Загружаю главу {urlChapter.Title.CoverQuotes()}");

            var chapterDoc = await GetChapter(urlChapter.Url);
            if (chapterDoc != default) {
                chapter.Title = urlChapter.Title;
                chapter.Images = await GetImages(chapterDoc, urlChapter.Url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;

                result.Add(chapter);
            }
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        var content = doc.QuerySelector("div.entry-content");
        var notice = content.QuerySelector("div.single-notice");
        return notice?.GetText() == "Для чтения купите главу." ? 
            default : 
            content.InnerHtml.AsHtmlDoc().RemoveNodes("div[class*=adv]");
    }

    private static Author GetAuthor() {
        return new Author("Ifreedom");
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.img-ranobe img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, imagePath)) : Task.FromResult(default(Image));
    }
}