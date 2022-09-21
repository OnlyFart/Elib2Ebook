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

public class Fb2TopGetter : GetterBase {
    public Fb2TopGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => null;

    public override bool IsSameUrl(Uri url) {
        return url.IsSameHost("https://fb2.top/".AsUri()) || url.IsSameHost("https://ladylib.top/".AsUri());
    }

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
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        return doc.QuerySelector("section").RemoveNodes("h3").InnerHtml.AsHtmlDoc();
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
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(url.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}