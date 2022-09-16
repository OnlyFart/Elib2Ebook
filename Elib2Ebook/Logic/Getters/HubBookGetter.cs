using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class HubBookGetter : GetterBase {
    public HubBookGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://hub-book.com/");

    protected override string GetId(Uri url) {
        return url.GetSegment(2);
    }

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
        var pages = int.Parse(doc.GetTextBySelector("span[itemprop=numberOfPages]"));
        var text = new StringBuilder();

        for (var i = 1; i <= pages; i++) {
            Console.WriteLine($"Получаю страницу {i}/{pages}");
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

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.b-book-image img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
    
    private static Author GetAuthor(HtmlDocument doc, Uri uri) {
        var a = doc.QuerySelector("a.b-book-user__name");
        return new Author(a.GetText(), uri.MakeRelativeUri(a.Attributes["href"].Value));
    }
}