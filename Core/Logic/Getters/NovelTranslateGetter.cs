using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class NovelTranslateGetter : GetterBase {
    public NovelTranslateGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://noveltranslate.com/");

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
        return lang == "novel" ? string.Empty : lang;
    }

    public override async Task<Book> Get(Uri url) {
        var lang = GetLang(url);
        url = string.IsNullOrWhiteSpace(lang) ? SystemUrl.MakeRelativeUri($"/novel/{GetId(url)}/") : SystemUrl.MakeRelativeUri($"/{lang}/novel/{GetId(url)}/");
        
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, lang, url),
            Title = doc.GetTextBySelector("h1"),
            Author = GetAuthor(doc, url),
            Annotation = GetAnnotation(doc),
            Lang = lang
        };
        
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, string lang, Uri url) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        foreach (var urlChapter in await GetToc(doc, lang, url)) {
            Config.Logger.LogInformation($"Загружаю главу {urlChapter.Title.CoverQuotes()}");

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
        return doc.QuerySelector("div.reading-content div.text-left").RemoveNodes("div.novel-before-content, div.tptn_counter, div.novel-after-content").InnerHtml.AsHtmlDoc();
    }

    private async Task<IEnumerable<UrlChapter>> GetToc(HtmlDocument doc, string lang, Uri url) {
        var lastChapter = url.MakeRelativeUri(doc.QuerySelector("div.listing-chapters_wrap li.wp-manga-chapter a").Attributes["href"].Value);
        var chapterDoc = await Config.Client.GetHtmlDocWithTriesAsync(lastChapter);

        var chapters = new List<UrlChapter>();
        foreach (var option in chapterDoc.QuerySelector("select.selectpicker_chapter").QuerySelectorAll("option")) {
            var name = option.GetText();
            var chapterUri = option.Attributes["data-redirect"].Value.AsUri();
            if (!string.IsNullOrWhiteSpace(lang) && !chapterUri.LocalPath.Trim('/').StartsWith(lang)) {
                chapterUri = url.MakeRelativeUri($"/{lang}/{chapterUri.LocalPath.Trim('/')}");
            }

            chapters.Add(new UrlChapter(chapterUri, name));
        }

        chapters.Reverse();
        return SliceToc(chapters);
    }

    private static string GetAnnotation(HtmlDocument doc) {
        return doc.QuerySelector("div.description-summary div.summary__content > p")?.InnerHtml;
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.summary_image img")?.Attributes["data-lazy-src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
    
    private static Author GetAuthor(HtmlDocument doc, Uri uri) {
        var a = doc.QuerySelector("div.summary-content div.author-content a");
        return a != default ? new Author(a.GetText(), uri.MakeRelativeUri(a.Attributes["href"].Value)) : new Author("NovelTranslate");
    }
}