using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Elib2Ebook.Extensions;

namespace Elib2Ebook.Logic.Getters; 

public class FicbookGetter : GetterBase {
    public FicbookGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ficbook.net/");

    protected override string GetId(Uri url) {
        return url.Segments[2].Trim('/');
    }

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = new Uri($"https://ficbook.net/readfic/{bookId}");
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);

        var title = doc.GetTextBySelector("h1.mb-10");
        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url, title),
            Title = title,
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div[itemprop=description]")?.InnerHtml
        };
            
        return book;
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("a.creator-nickname");
        return new Author(a.GetText(), new Uri(url, a.Attributes["href"].Value));
    }
        
    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("fanfic-cover")?.Attributes["src-original"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url, string title) {
        var result = new List<Chapter>();
            
        foreach (var ficbookChapter in GetToc(doc, url, title)) {
            Console.WriteLine($"Загружаю главу {ficbookChapter.Title.CoverQuotes()}");
            var chapter = new Chapter();
            var chapterDoc = await GetChapter(ficbookChapter);
            chapter.Images = await GetImages(chapterDoc, url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = ficbookChapter.Title;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(UrlChapter urlChapter) {
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url);
        var content = doc.QuerySelector("#content").RemoveNodes("div");
        using var sr = new StringReader(content.InnerText.HtmlDecode());

        var text = new StringBuilder();
        while (true) {
            var line = await sr.ReadLineAsync();
            if (line == null) {
                break;
            }

            if (string.IsNullOrWhiteSpace(line)) {
                continue;
            }
                
            text.Append(line.HtmlEncode().CoverTag("p"));
        }

        return text.AsHtmlDoc();
    }

    private static IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url, string title) {
        var links = doc.QuerySelectorAll("li.part");
        if (links.Count == 0) {
            yield return new UrlChapter(url, title);
        } else {
            foreach (var li in links) {
                var a = li.QuerySelector("a.part-link.visit-link");
                if (a != null) {
                    yield return new UrlChapter(new Uri(url, a.Attributes["href"].Value), li.GetTextBySelector("h3"));
                }
            }
        }
    }
}