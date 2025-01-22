using System;
using System.Collections.Generic;
using System.Linq;
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

public class JaomixGetter(BookGetterConfig config) : GetterBase(config) {
    protected override Uri SystemUrl => new("https://jaomix.ru/");

    protected override string GetId(Uri url) => url.GetSegment(1);

    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri($"/{GetId(url)}/");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1"),
            Author = new Author("Jaomix")
        };
            
        return book;
    }
    
    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        foreach (var jaomixChapter in GetToc(doc, url)) {
            Config.Logger.LogInformation($"Загружаю главу {jaomixChapter.Title.CoverQuotes()}");
            var chapter = new Chapter();
            var chapterDoc = await GetChapter(jaomixChapter.Url);
            chapter.Images = await GetImages(chapterDoc, url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = jaomixChapter.Title;

            result.Add(chapter);
            await Task.Delay(500);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        while (doc.QuerySelector("div.themeform div.h-captcha, div.themeform div.but-captcha") != null) {
            Config.Logger.LogInformation($"Обнаружена капча. Перейдите по ссылке {url}, введите капчу и нажмите Enter...");
            Console.Read();
            doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        }
        
        var sb = new StringBuilder();
            
        foreach (var node in doc.QuerySelector("div.themeform").ChildNodes) {
            if (node.Name != "br" && node.Name != "script" && !string.IsNullOrWhiteSpace(node.InnerHtml) && node.Attributes["class"]?.Value?.Contains("adblock-service") == null) {
                var tag = node.Name == "#text" ? "p" : node.Name;
                sb.Append(node.InnerHtml.HtmlDecode().CoverTag(tag));
            }
        }

        return sb.AsHtmlDoc();
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        var chapters = ParseChapters(doc, url).ToList();
        Config.Logger.LogInformation($"Получено {chapters.Count} глав");
        
        return SliceToc(chapters, c => c.Title);
    }
        
    private Task<TempFile> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.img-book img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }

    private static IEnumerable<UrlChapter> ParseChapters(HtmlDocument doc, Uri url) {
        return doc.QuerySelectorAll("form.download-chapter .hiddenstab .flex-dow-txt a").Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.InnerText.Trim())).Reverse();
    }
}