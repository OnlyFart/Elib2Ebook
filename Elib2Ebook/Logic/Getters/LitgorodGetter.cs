using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class LitgorodGetter : GetterBase {
    public LitgorodGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://litgorod.ru/");
    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        var uri = new Uri($"https://litgorod.ru/books/view/{bookId}");
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(uri);

        var book = new Book(uri) {
            Cover = await GetCover(doc, uri),
            Chapters = await FillChapters(uri, bookId),
            Title = doc.GetTextBySelector("p.info_title"),
            Author = GetAuthor(doc, uri),
            Annotation = doc.QuerySelector("div.annotation_footer--content p.item_info")?.InnerHtml,
            Seria = GetSeria(doc)
        };
            
        return book;
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("a.info_author");
        return new Author(a.GetTextBySelector(), new Uri(url, a.Attributes["href"].Value));
    }

    private static Seria GetSeria(HtmlDocument doc) {
        var circle = doc.GetTextBySelector("p.info_desciption--circle");
        if (!string.IsNullOrWhiteSpace(circle)) {
            return new Seria {
                Name = circle[4..].Trim()
            };
        }

        return default;
    }
    
    private async Task<IEnumerable<Chapter>> FillChapters(Uri uri, string bookId) {
        var result = new List<Chapter>();

        foreach (var bookChapter in await GetChapters(bookId)) {
            var chapter = new Chapter();
            Console.WriteLine($"Загружаю главу {bookChapter.Title.CoverQuotes()}");
            
            var doc = await GetChapter(bookChapter.Id, bookId);

            if (doc != default) {
                chapter.Title = bookChapter.Title;
                chapter.Images = await GetImages(doc, uri);
                chapter.Content = doc.DocumentNode.InnerHtml;

                result.Add(chapter);
            }
        }

        return result;
    }
    
    private async Task<HtmlDocument> GetChapter(string chapterId, string bookId) {
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://litgorod.ru/books/read/{bookId}?chapter={chapterId}"));
        var li = doc.QuerySelectorAll("ul.reader__pagen__ul__wrap li");
        var pages = int.Parse(li.Count > 0 ? li.Last().InnerText : "1");

        var sb = new StringBuilder();
        for (var i = 1; i <= pages; i++) {
            doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://litgorod.ru/books/read/{bookId}?chapter={chapterId}&page={i}"));
            sb.Append(doc.QuerySelector("div.reader__content__wrap").RemoveNodes("div.reader__content__title").InnerHtml.HtmlDecode());
        }

        return sb.AsHtmlDoc();
    }

    private async Task<IEnumerable<IdChapter>> GetChapters(string bookId) {
        var uri = new Uri($"https://litgorod.ru/books/read/{bookId}");
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(uri);
        return doc
            .QuerySelectorAll("div.select__block ul.select__list li")
            .Select((l, i) => new IdChapter((i + 1).ToString(), l.InnerText.Trim()));
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.annotation_main--poster img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, imagePath)) : Task.FromResult(default(Image));
    }
}