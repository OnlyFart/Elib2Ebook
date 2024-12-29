using System;
using System.Collections.Generic;
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

public class HubBookGetter(BookGetterConfig config) : GetterBase(config) {
    protected override Uri SystemUrl => new("https://hub-book.com/");

    protected override string GetId(Uri url) => url.GetSegment(2);

    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri($"/books/{GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var author = GetAuthor(doc, url);
        var title = doc.GetTextBySelector("h1").Replace(author.Name, string.Empty).Trim('-', ' ');

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url, title),
            Title = title,
            Author = author,
            Annotation = doc.QuerySelector("div.b-book-desc div.more_text")?.InnerHtml,
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url, string title) {
        if (Config.Options.NoChapters) {
            return [];
        }
        
        var pages = int.Parse(doc.GetTextBySelector("span[itemprop=numberOfPages]"));
        var text = new StringBuilder();

        for (var i = 1; i <= pages; i++) {
            Config.Logger.LogInformation($"Получаю страницу {i}/{pages}");
            doc = await Config.Client.GetHtmlDocWithTriesAsync(url.AppendSegment($"/toread/page-{i}"));
            text.Append(doc.QuerySelector("div.b-reader-text__container").InnerHtml.HtmlDecode());
        }

        var chapter = new Chapter();
        var chapterDoc = text.AsHtmlDoc();
        
        chapter.Title = title;
        chapter.Images = await GetImages(chapterDoc, url);
        chapter.Content = chapterDoc.DocumentNode.InnerHtml;

        return new[] { chapter };
    }

    private Task<TempFile> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.b-book-image img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
    
    private static Author GetAuthor(HtmlDocument doc, Uri uri) {
        var a = doc.QuerySelector("a.b-book-user__name");
        return new Author(a.GetText(), uri.MakeRelativeUri(a.Attributes["href"].Value));
    }
}