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

public class SamlibGetter : GetterBase {
    private const string START_BOOK_PATTERN = "Собственно произведение";
    private const string ABOUT_BLOCK_PATTERN = "Блок описания произведения";
    private const string END_BOOK_PATTERN = "-----------------------------------------------";

    private const string START_LINK_BLOCK_PATTERN = "Блок ссылок на произведения";
    private const string END_LINK_BLOCK_PATTERN = "Подножие";
        
    public SamlibGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("http://samlib.ru/");
    public override async Task<Book> Get(Uri url) {
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);

        var title = doc.GetTextBySelector("h2, h3 font");
        var book = new Book(url) {
            Cover = null,
            Chapters = await FillChapters(doc, url, title),
            Title = title,
            Author = new Author("Samlib")
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url, string title) {
        var result = new List<Chapter>();
            
        foreach (var urlChapter in GetChapters(doc, url, title)) {
            Console.WriteLine($"Загружаю главу {urlChapter.Title.CoverQuotes()}");
            result.Add(await GetChapter(urlChapter));
        }

        return result;
    }

    private IEnumerable<UrlChapter> GetChapters(HtmlDocument doc, Uri url, string title) {
        var content = GetStringBetween(doc.Text, START_LINK_BLOCK_PATTERN, END_LINK_BLOCK_PATTERN);
        if (string.IsNullOrWhiteSpace(content)) {
            yield return new UrlChapter(url, title);
        } else {
            foreach (var a in content.AsHtmlDoc().QuerySelectorAll("li > a")) {
                yield return new UrlChapter(new Uri(url, a.Attributes["href"].Value), a.GetTextBySelector());
            }
        }
    }

    private static string GetStringBetween(string origin, string startPattern, string endPattern) {
        var start = origin.IndexOf(startPattern, StringComparison.InvariantCultureIgnoreCase);
        if (start == -1) {
            return string.Empty;
        }
            
        start = origin.IndexOf(">", start, StringComparison.InvariantCultureIgnoreCase) + 1;

        var stop = origin.IndexOf(endPattern, start, StringComparison.InvariantCultureIgnoreCase);
        for (var i = stop;; i--) {
            if (origin[i] == '<') {
                stop = i - 1;
                break;
            }
        }

        return origin[start..stop];
    }
    
    private static string GetBookContent(string origin) {
        var start = origin.IndexOf(START_BOOK_PATTERN, StringComparison.InvariantCultureIgnoreCase);
        if (start == -1) {
            return string.Empty;
        }

        var about = origin.LastIndexOf(ABOUT_BLOCK_PATTERN, StringComparison.OrdinalIgnoreCase);
        if (about == -1) {
            return string.Empty;
        }
            
        start = origin.IndexOf(">", start, StringComparison.InvariantCultureIgnoreCase) + 1;
        var stop = origin.LastIndexOf(END_BOOK_PATTERN, about, StringComparison.InvariantCultureIgnoreCase);
        
        for (var i = stop;; i--) {
            if (origin[i] == '<') {
                stop = i - 1;
                break;
            }
        }

        return origin[start..stop].HtmlDecode();
    }

    private async Task<Chapter> GetChapter(UrlChapter urlChapter) {
        var chapter = new Chapter();

        var doc = await _config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url);
        doc.LoadHtml(GetBookContent(doc.Text));
            
        var sr = new StringReader(doc.DocumentNode.InnerHtml.HtmlDecode());
        var text = new StringBuilder();
        while (true) {
            var line = await sr.ReadLineAsync();
            if (line == null) {
                break;
            }

            if (string.IsNullOrWhiteSpace(line)) {
                continue;
            }
                
            var htmlDoc = line.AsHtmlDoc();
            foreach (var node in htmlDoc.DocumentNode.ChildNodes) {
                if (!string.IsNullOrWhiteSpace(node.InnerText) || node.QuerySelector("img") != null) {
                    text.Append($"<p>{node.InnerHtml.HtmlDecode().Trim()}</p>");
                }
            }
        }
            
        var chapterDoc = text.AsHtmlDoc();
        chapter.Images = await GetImages(chapterDoc, urlChapter.Url);
        chapter.Content = chapterDoc.DocumentNode.InnerHtml;
        chapter.Title = urlChapter.Title;

        return chapter;
    }
}