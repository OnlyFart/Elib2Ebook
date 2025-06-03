using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters;

public class MangaMammyGetter(BookGetterConfig config) : GetterBase(config) {
    protected override Uri SystemUrl => new("https://mangamammy.ru/");
    
    protected override string GetId(Uri url) => url.GetSegment(2);
    
    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri("/manga/" + GetId(url));
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1"),
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div.manga-excerpt")?.InnerHtml
        };
            
        return book;
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("div.author-content a");
        return a == default ? new Author("MangaMammy") : new Author(a.GetText(), url.MakeRelativeUri(a.Attributes["href"].Value));
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

            if (chapterDoc != default) {
                chapter.Images = await GetImages(chapterDoc, url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }
            
            result.Add(chapter);
        }

        return result;
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        var result = doc
            .QuerySelectorAll("ul.sub-chap-list li.wp-manga-chapter > a")
            .Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.GetText().ReplaceNewLine()))
            .Reverse()
            .ToList();
        
        return SliceToc(result, c => c.Title);
    }

    private async Task<HtmlDocument> GetChapter(UrlChapter urlChapter) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url);

        var images = doc
            .QuerySelectorAll("img.wp-manga-chapter-img")
            .Select(img => img.Attributes["src"].Value)
            .ToList();

        var sb = new StringBuilder();

        foreach (var image in images)
        {
            sb.Append($"<img src='{image}'/>");
        }

        return sb.AsHtmlDoc();
    }

    private Task<TempFile> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.summary_image img")?.Attributes["data-lazy-src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
}