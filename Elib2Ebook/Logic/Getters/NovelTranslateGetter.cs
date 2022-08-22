using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class NovelTranslateGetter : GetterBase {
    public NovelTranslateGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://noveltranslate.com/novel/raiders-counterattack-quick-transmigration/chapter-2/");

    protected override string GetId(Uri url) {
        for (var i = 0; i < url.Segments.Length; i++) {
            if (url.Segments[i].StartsWith("novel")) {
                return url.Segments[i + 1].Trim('/');
            }
        }

        throw new Exception($"Не могу определить ID книги {url}");
    }

    private static string GetLang(Uri url) {
        var lang = url.Segments[1].Trim('/');
        return lang == "novel" ? "en" : lang;
    }

    public override async Task<Book> Get(Uri url) {
        var lang = GetLang(url);
        url = new Uri($"https://noveltranslate.com/{lang}/novel/{GetId(url)}/");
        
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1"),
            Author = GetAuthor(doc, url),
            Annotation = GetAnnotation(doc),
            Lang = lang
        };
        
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();

        foreach (var urlChapter in GetToc(doc, url)) {
            Console.WriteLine($"Загружаю главу {urlChapter.Title.CoverQuotes()}");

            var chapter = new Chapter {
                Title = urlChapter.Title
            };
            
            var chapterDoc = await GetChapter(urlChapter);
            chapter.Images = await GetImages(chapterDoc, SystemUrl);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(UrlChapter urlChapter) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url);
        return doc.QuerySelector("div.reading-content div.text-left").InnerHtml.AsHtmlDoc();
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        var result = doc.QuerySelectorAll("div.listing-chapters_wrap li.wp-manga-chapter a").Select(a => new UrlChapter(new Uri(url, a.Attributes["href"].Value), a.GetText())).ToList();
        result.Reverse();
        return SliceToc(result);
    }

    private static string GetAnnotation(HtmlDocument doc) {
        return doc.QuerySelector("div.description-summary div.summary__content > p")?.InnerHtml;
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.summary_image img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, imagePath)) : Task.FromResult(default(Image));
    }
    
    private static Author GetAuthor(HtmlDocument doc, Uri uri) {
        var a = doc.QuerySelector("div.summary-content div.author-content a");
        return a != default ? new Author(a.GetText(), new Uri(uri, a.Attributes["href"].Value)) : new Author("NovelTranslate");
    }
}