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
        return url.IsSameHost(new Uri("https://fb2.top/")) || url.IsSameHost(new Uri("https://ladylib.top/"));
    }

    protected override string GetId(Uri url) {
        return url.Segments[1].Trim('/');
    }

    public override async Task<Book> Get(Uri url) {
        url = new Uri($"https://{url.Host}/{GetId(url)}");
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
        return doc.QuerySelector("section").RemoveNodes("h3").InnerHtml.AsHtmlDoc();
    }

    private static IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        return doc.QuerySelectorAll("div.card-body li a").Select(a => new UrlChapter(new Uri(url, a.Attributes["href"].Value), a.GetText()));
    }

    private static Seria GetSeria(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("div.book-info-body a[href*=/series/]");
        if (a != default) {
            return new Seria {
                Name = a.GetText(),
                Url = new Uri(url, a.Attributes["href"].Value)
            };
        }

        return default;
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("div.book-info-body a[href*=/authors/]");
        return a != default ? 
            new Author(a.GetText().ReplaceNewLine(), new Uri(url, a.Attributes["href"].Value)) : 
            new Author("Fb2Top");
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri url) {
        var imagePath = doc.QuerySelector("img.book-info-poster-img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(url, imagePath)) : Task.FromResult(default(Image));
    }
}