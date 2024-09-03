using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Core.Logic.Getters; 

public class BookInBookGetter : GetterBase {
    public BookInBookGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://bookinbook.ru/");

    protected override string GetId(Uri url) => url.GetQueryParameter("id");

    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri($"book?id={GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        
        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("span.main-info__name"),
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div.annotation-form__text")?.InnerHtml,
            Seria = GetSeria(doc)
        };
            
        return book;
    }
    
    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri uri) {
        var result = new List<Chapter>();

        foreach (var bookChapter in GetToc(doc, uri)) {
            var chapter = new Chapter();
            Console.WriteLine($"Загружаю главу {bookChapter.Title.CoverQuotes()}");
            
            var chapterDoc = await GetChapter(bookChapter.Url);

            chapter.Title = bookChapter.Title;
            chapter.Images = await GetImages(chapterDoc, uri);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;

            result.Add(chapter);
        }

        return result;
    }
    
    private async Task<HtmlDocument> GetChapter(Uri url) {

        var sb = new StringBuilder();
        for (var i = 1; ; i++) {
            var uri = SystemUrl.MakeRelativeUri($"read?id={url.GetQueryParameter("id")}&chapter={url.GetQueryParameter("chapter")}&page={i}");
            var response = await Config.Client.GetWithTriesAsync(uri, TimeSpan.FromMilliseconds(100));
            if (response == default) {
                return sb.AsHtmlDoc();
            }

            var doc = await response.Content.ReadAsStreamAsync().ContinueWith(t => t.Result.AsHtmlDoc());
            var text = doc.GetTextBySelector("#PAGE_TEXT").Deserialize<string>().HtmlDecode();
            using var sr = new StringReader(text);
            
            while (true) {
                var line = await sr.ReadLineAsync();
                if (line == null) {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                
                sb.Append(line.CoverTag("p"));
            }
        }
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        var urlChapters = doc.QuerySelectorAll("a.chapters-form__chapter").Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.GetText().ReplaceNewLine())).ToList();
        return SliceToc(urlChapters);
    }
    
    private static Seria GetSeria(HtmlDocument doc) {
        var a = doc.QuerySelector("span.series-info__name");
        if (a != default) {
            return new Seria {
                Name = a.GetText()
            };
        }

        return default;
    }
    
    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("img.book-cover__image")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
    
    private static Author GetAuthor(HtmlDocument doc, Uri uri) {
        var a = doc.QuerySelector("a.author");
        return new Author(a.GetText(), uri.MakeRelativeUri(a.Attributes["href"].Value));
    }
}