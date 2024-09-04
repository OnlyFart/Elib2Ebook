using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters.Freedom; 

public abstract class FreedomGetterBase : GetterBase{
    protected FreedomGetterBase(BookGetterConfig config) : base(config) { }

    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);
        url = SystemUrl.MakeRelativeUri($"/ranobe/{GetId(url)}/");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        
        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1.entry-title"),
            Author = GetAuthor()
        };
            
        return book;
    }

    private async Task<Uri> GetMainUrl(Uri url) {
        if (url.GetSegment(1) == "ranobe") {
            return url;
        }

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        return url.MakeRelativeUri(doc.QuerySelector("div.bun2 a").Attributes["href"].Value);
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        var result = doc
            .QuerySelectorAll("div.li-col1-ranobe > a")
            .Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.GetText()))
            .Reverse()
            .ToList();
        
        return SliceToc(result);
    }
    
    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri uri) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        foreach (var urlChapter in GetToc(doc, uri)) {
            var chapter = new Chapter {
                Title = urlChapter.Title
            };

            Config.Logger.LogInformation($"Загружаю главу {urlChapter.Title.CoverQuotes()}");

            var chapterDoc = await GetChapter(urlChapter.Url);
            if (chapterDoc != default) {
                chapter.Images = await GetImages(chapterDoc, urlChapter.Url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }
            
            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        var content = doc.QuerySelector("div.entry-content");
        var notice = content.QuerySelector("div.single-notice");
        return notice?.GetText() == "Для чтения купите главу." ? 
            default : 
            content.InnerHtml.AsHtmlDoc().RemoveNodes("div[class*=adv]");
    }

    private static Author GetAuthor() {
        return new Author("Ifreedom");
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.img-ranobe img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}