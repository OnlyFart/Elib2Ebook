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

public class OnlineKnigiGetter : GetterBase {
    public OnlineKnigiGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://online-knigi.com.ua/");

    protected override string GetId(Uri url) {
        return url.Segments[2].Trim('/');
    }

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = new Uri($"https://online-knigi.com.ua/kniga/{bookId}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var title = doc.GetTextBySelector("h1");
        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(bookId, title),
            Title = title,
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div.book_description")?.InnerHtml,
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(string bookId, string title) {
        var chapter = new Chapter();
        var sb = new StringBuilder();
        var pages = await GetPages(bookId);

        for (var i = 1; i <= pages; i++) {
            Console.WriteLine($"Получаю страницу {i}/{pages}");

            var uri = new Uri($"https://online-knigi.com.ua/page/{bookId}?page={i}");
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(uri);
            sb.Append(doc.QuerySelector("div.content_book").InnerHtml.HtmlDecode());
        }

        var chapterDoc = sb.AsHtmlDoc().RemoveNodes("div.adv_text");

        chapter.Title = title;
        chapter.Content = chapterDoc.DocumentNode.InnerHtml;

        return new[] { chapter };
    }

    private async Task<int> GetPages(string bookId) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://online-knigi.com.ua/page/{bookId}"));
         return int.Parse(new Uri(doc.QuerySelector("li.last a").Attributes["href"].Value).GetQueryParameter("page"));
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("meta[property=og:image]")?.Attributes["content"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, imagePath)) : Task.FromResult(default(Image));
    }
    
    private static Author GetAuthor(HtmlDocument doc, Uri uri) {
        var a = doc.QuerySelector("div[itemprop=author] a");
        return new Author(a.GetText(), new Uri(uri, a.Attributes["href"].Value));
    }
}