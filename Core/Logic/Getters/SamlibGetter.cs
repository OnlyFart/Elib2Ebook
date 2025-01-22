using System;
using System.Collections.Generic;
using System.IO;
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

public class SamlibGetter(BookGetterConfig config) : GetterBase(config) {
    private const string START_BOOK_PATTERN = "Собственно произведение";
    private const string ABOUT_BLOCK_PATTERN = "Блок описания произведения";
    private const string END_BOOK_PATTERN = "-----------------------------------------------";

    private const string START_LINK_BLOCK_PATTERN = "Блок ссылок на произведения";
    private const string END_LINK_BLOCK_PATTERN = "Подножие";

    private static Encoding _encoding = Encoding.GetEncoding(1251);

    protected override Uri SystemUrl => new("http://samlib.ru/");
    public override async Task<Book> Get(Uri url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url, _encoding);

        var title = doc.GetTextBySelector("h2, h3 font");
        var book = new Book(url) {
            Cover = null,
            Chapters = await FillChapters(doc, url, title),
            Title = title,
            Author = GetAuthor(doc, url)
        };
            
        return book;
    }
    
    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var def = new Author("Samlib");
        
        var h3 = doc.QuerySelector("h3");
        if (h3 == default) {
            return def;
        }

        var a = h3.QuerySelector("a[href]");
        if (a == default) {
            return def;
        }

        return new Author(h3.RemoveNodes("small").GetText().Trim(':'), url.MakeRelativeUri(a.Attributes["href"].Value));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url, string title) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }
            
        foreach (var urlChapter in GetToc(doc, url, title)) {
            Config.Logger.LogInformation($"Загружаю главу {urlChapter.Title.CoverQuotes()}");
            result.Add(await GetChapter(urlChapter));
        }

        return result;
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url, string title) {
        var content = GetStringBetween(doc.Text, START_LINK_BLOCK_PATTERN, END_LINK_BLOCK_PATTERN);
        if (string.IsNullOrWhiteSpace(content)) {
            yield return new UrlChapter(url, title);
        } else {
            foreach (var a in content.AsHtmlDoc().QuerySelectorAll("li > a")) {
                yield return new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.GetText());
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

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url, _encoding);
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
                    text.Append(node.InnerHtml.HtmlDecode().CoverTag("p"));
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