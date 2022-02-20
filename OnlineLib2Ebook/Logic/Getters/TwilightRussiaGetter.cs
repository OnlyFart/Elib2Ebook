using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using OnlineLib2Ebook.Configs;
using OnlineLib2Ebook.Extensions;
using OnlineLib2Ebook.Types.Book;

namespace OnlineLib2Ebook.Logic.Getters; 

public class TwilightRussiaGetter : GetterBase {
    public TwilightRussiaGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://twilightrussia.ru/");
    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = new Uri($"https://twilightrussia.ru/forum/{bookId}");
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book {
            Cover = null,
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("a.forumBarA"),
            Author = "twilightrussia"
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var post = doc.QuerySelectorAll("td.content-block-forum");
        
        var result = new List<Chapter>();
        foreach (var a in post.QuerySelectorAll("a[href*='/publ/']")) {
            Console.WriteLine($"Загружаем главу {a.GetTextBySelector()}");
            var href = new Uri(url, a.Attributes["href"].Value); 
            result.Add(await GetChapter(href, a.GetTextBySelector()));
        }

        return result;
    }
    
    private async Task<Chapter> GetChapter(Uri url, string title) {
        var chapter = new Chapter();

        var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
        
        var text = new StringBuilder();
        foreach (var node in doc.QuerySelector("#msgd").ChildNodes) {
            var nodeText = node.GetTextBySelector();
            if (!string.IsNullOrWhiteSpace(nodeText)) {
                text.Append($"<p>{nodeText}</p>");
            }
        }
        
            
        var chapterDoc = text.ToString().HtmlDecode().AsHtmlDoc();
        chapter.Images = await GetImages(chapterDoc, url);
        chapter.Content = chapterDoc.DocumentNode.InnerHtml;
        chapter.Title = title;

        return chapter;
    }
}