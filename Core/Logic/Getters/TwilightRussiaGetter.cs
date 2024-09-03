using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class TwilightRussiaGetter : GetterBase {
    public TwilightRussiaGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://twilightrussia.ru/");
    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);
        url = SystemUrl.MakeRelativeUri($"/forum/{GetId(url)}");
        
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = null,
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("a.forumBarA"),
            Author = new Author(doc.GetTextBySelector("span[class^='forum_nik']"))
        };
            
        return book;
    }

    private async Task<Uri> GetMainUrl(Uri url) {
        if (url.ToString().Contains("/publ/")) {
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
            var a = doc.QuerySelector("#msgd ~ a[href]");
            return a.Attributes["href"].Value.AsUri();
        }

        return url;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        if (Config.Options.NoChapters) {
            return [];
        }
        
        var post = doc.QuerySelectorAll("td.content-block-forum");
        
        var result = new List<Chapter>();
        foreach (var a in post.QuerySelectorAll("a[href*='/publ/']")) {
            var title = a.GetText();
            if (string.IsNullOrWhiteSpace(title)) {
                continue;
            }
            
            Config.Logger.LogInformation($"Загружаю главу {title}");
            var href = url.MakeRelativeUri(a.Attributes["href"].Value);
            result.Add(await GetChapter(href, title));
        }

        return result;
    }
    
    private async Task<Chapter> GetChapter(Uri url, string title) {
        var chapter = new Chapter();

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        
        var text = new StringBuilder();
        foreach (var node in doc.QuerySelector("#msgd").ChildNodes) {
            var img = node.QuerySelector("img[src]");
            if (img != null) {
                text.Append($"<img src='{img.Attributes["src"].Value}'/>");
                continue;
            }
            
            var nodeText = node.GetText();
            if (!string.IsNullOrWhiteSpace(nodeText)) {
                text.Append(nodeText.CoverTag("p"));
            }
        }
        
            
        var chapterDoc = text.AsHtmlDoc();
        chapter.Images = await GetImages(chapterDoc, url);
        chapter.Content = chapterDoc.DocumentNode.InnerHtml;
        chapter.Title = title;

        return chapter;
    }
}