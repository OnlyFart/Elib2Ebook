using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class AcomicsGetter : GetterBase{
    public AcomicsGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://acomics.ru");

    protected override string GetId(Uri url) {
        return url.Segments[1].Trim('/');
    }

    public override async Task<Book> Get(Uri url) {
        var id = GetId(url);
        url = new Uri($"https://acomics.ru/{id}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var title = doc.QuerySelector("meta[property=og:title]").Attributes["content"].Value;
        var book = new Book(url) {
            Chapters = await FillChapters(id, title, url),
            Title = title,
            Author = GetAuthor(doc, url),
            Annotation = GetAnnotation(doc)
        };

        book.Cover = book.Chapters.FirstOrDefault()?.Images.FirstOrDefault();
            
        return book;
    }

    private static string GetAnnotation(HtmlDocument doc) {
        return doc.QuerySelector("div.serial-description div.s-gap")?.NextSibling?.InnerHtml ?? doc.QuerySelector("div.about-summary p")?.InnerHtml;
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("article.authors a.username");
        if (a == default) {
            foreach (var p in doc.QuerySelectorAll("div.about-summary p")) {
                var b = p.GetTextBySelector("b");
                if (b == "Автор:") {
                    a = p.QuerySelector("a");
                    return new Author(a.GetText(), new Uri(url, a.Attributes["href"].Value));
                }
            }
        }
        
        return new Author(a.GetText(), new Uri(url, a.Attributes["href"].Value));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(string bookId, string title, Uri url) {
        var chapter = new Chapter {
            Title = title
        };

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(new Uri(SystemUrl, $"{bookId}/{1}"));
        var pages = int.Parse(doc.GetTextBySelector("span.issueNumber").Split("/").Last());
        var sb = new StringBuilder();
        for (var i = 1; i <= pages; i++) {
            Console.WriteLine($"Получаю страницу {i}/{pages}");
            var response = await Config.Client.GetHtmlDocWithTriesAsync(new Uri(SystemUrl, $"{bookId}/{i}"));
            var img = response.QuerySelector("#mainImage");
            var src = new Uri(url, img.Attributes["src"].Value);
            sb.Append($"<img src='{src}'/>");
        }

        var chapterDoc = sb.AsHtmlDoc();
        chapter.Images = await GetImages(chapterDoc, url);
        chapter.Content = chapterDoc.DocumentNode.InnerHtml;

        return new []{ chapter };
    }
}