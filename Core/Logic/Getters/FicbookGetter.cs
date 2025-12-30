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

public class FicbookGetter(BookGetterConfig config) : GetterBase(config) {
    protected override Uri SystemUrl => new("https://ficbook.net/");
    
    protected Uri _appUrl => new("https://fanficlets.xyz/");

    protected override string GetId(Uri url) => url.GetSegment(2);

    public override async Task Init() {
        await base.Init();
        Config.Client.DefaultRequestHeaders.Remove("User-Agent");
        Config.Client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (iPhone); iOS CPU iPhone OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1 ||| ficbook (1.3.2) | iOS (26.1) | iPhone 13 | ao2gvvnhkv0t22lo");
    }

    public override async Task<Book> Get(Uri url) {
        var appUrl = _appUrl.MakeRelativeUri($"/readfic/{GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(appUrl);

        var title = doc.GetTextBySelector("h1");
        var book = new Book(SystemUrl.MakeRelativeUri($"/readfic/{GetId(url)}")) {
            Cover = await GetCover(doc, appUrl),
            Chapters = await FillChapters(doc, appUrl, title),
            Title = title,
            Author = GetAuthor(doc, appUrl),
            Annotation = doc.QuerySelector("div[itemprop=description]")?.InnerHtml
        };
            
        return book;
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("a.creator-username");
        return new Author(a.GetText(), url.MakeRelativeUri(a.Attributes["href"].Value));
    }
        
    private Task<TempFile> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("fanfic-cover")?.Attributes["src-original"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url, string title) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }
            
        foreach (var ficbookChapter in GetToc(doc, url, title)) {
            Config.Logger.LogInformation($"Загружаю главу {ficbookChapter.Title.CoverQuotes()}");
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
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url);
        
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

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url, string title) {
        var result = new List<UrlChapter>();
        
        var links = doc.QuerySelectorAll("li.part");
        if (links.Count == 0) {
            result.Add(new UrlChapter(url, title));
        } else {
            foreach (var li in links) {
                var a = li.QuerySelector("a.part-link");
                if (a != null) {
                    result.Add(new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), li.GetTextBySelector("h3")));
                }
            }
        }
        
        return SliceToc(result, c => c.Title);
    }
}
