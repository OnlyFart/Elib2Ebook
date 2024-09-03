using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Core.Logic.Getters.Fb2Top; 

public abstract class Fb2TopGetterBase : GetterBase {
    protected Fb2TopGetterBase(BookGetterConfig config) : base(config) { }

    protected override string GetId(Uri url) => url.GetSegment(1);

    public override async Task<Book> Get(Uri url) {
        url = url.MakeRelativeUri(GetId(url));
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        
        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1.book-title"),
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div.annotation")?.InnerHtml,
            Seria = GetSeria(doc, url)
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();

        foreach (var urlChapter in GetToc(doc, url)) {
            Console.WriteLine($"Загружаю главу {urlChapter.Title.CoverQuotes()}");
            var chapter = new Chapter {
                Title = urlChapter.Title
            };

            var chapterDoc = await GetChapter(urlChapter.Url);
            if (chapterDoc != default) {
                chapter.Images = await GetImages(chapterDoc, urlChapter.Url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }
            
            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri url) {
        var doc = await Config.Client.GetStringAsync(url);
        doc = doc.Replace("</h2>", "</h3>");
        doc = doc.Replace("<h2>", "<h3>");
        return doc.AsHtmlDoc().QuerySelector("section").RemoveNodes("h3").InnerHtml.AsHtmlDoc();
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        var urlChapters = doc.QuerySelectorAll("div.card-body li a").Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.GetText())).ToList();
        return SliceToc(urlChapters);
    }

    private static Seria GetSeria(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("div.book-info-body a[href*=/series/]");
        if (a != default) {
            return new Seria {
                Name = a.GetText(),
                Url = url.MakeRelativeUri(a.Attributes["href"].Value)
            };
        }

        return default;
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("div.book-info-body a[href*=/authors/]");
        return a != default ? 
            new Author(a.GetText().ReplaceNewLine(), url.MakeRelativeUri(a.Attributes["href"].Value)) : 
            new Author("Fb2Top");
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri url) {
        var imagePath = doc.QuerySelector("img.book-info-poster-img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(url.MakeRelativeUri(imagePath.HtmlDecode())) : Task.FromResult(default(Image));
    }
}