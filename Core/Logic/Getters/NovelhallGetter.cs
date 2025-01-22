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
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters;

public class NovelhallGetter(BookGetterConfig config) : GetterBase(config) {
    protected override Uri SystemUrl => new("https://www.novelhall.com/");

    protected override string GetId(Uri url) {
        return url.GetSegment(1);
    }

    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri(GetId(url));
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1"),
            Author = GetAuthor(doc),
            Annotation = GetAnnotation(doc),
            Lang = "en"
        };
        
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        foreach (var urlChapter in GetToc(doc, url)) {
            Config.Logger.LogInformation($"Загружаю главу {urlChapter.Title.CoverQuotes()}");

            var chapter = new Chapter {
                Title = urlChapter.Title
            };
            
            var chapterDoc = await GetChapter(urlChapter);
            chapter.Images = await GetImages(chapterDoc, SystemUrl);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(UrlChapter urlChapter) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url);
        return doc.QuerySelector("div.entry-content").InnerHtml.AsHtmlDoc();
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        var chapters = doc.QuerySelectorAll("#morelist ul li a[href]").Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.GetText())).ToList();
        return SliceToc(chapters, c => c.Title);
    }

    private static string GetAnnotation(HtmlDocument doc) {
        return doc.QuerySelector("div.intro span.js-close-wrap").RemoveNodes(".blue")?.InnerHtml;
    }

    private Task<TempFile> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.book-img img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
    
    private static Author GetAuthor(HtmlDocument doc) {
        var a = doc.QuerySelector("meta[property='books:author']");
        return a != default ? new Author(a.Attributes["content"].Value) : new Author("NovelHall");
    }
}