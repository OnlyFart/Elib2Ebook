using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class AcomicsGetter : GetterBase {
    public AcomicsGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://acomics.ru");

    protected override string GetId(Uri url) => url.GetSegment(1);

    public override async Task Init() {
        await base.Init();
        Config.CookieContainer.Add(SystemUrl, new Cookie("ageRestrict", "18"));
    }

    public override async Task<Book> Get(Uri url) {
        var id = GetId(url);
        url = SystemUrl.MakeRelativeUri(id);

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri($"{id}/1"));

        var title = doc.QuerySelector("meta[property=og:title]").Attributes["content"].Value;
        var book = new Book(url) {
            Chapters = await FillChapters(doc, id, title, url),
            Title = title,
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div.serial-description div.s-gap")?.NextSibling?.InnerHtml
        };

        book.Cover = book.Chapters.FirstOrDefault()?.Images.FirstOrDefault();
            
        return book;
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("article.authors a.username");
        return new Author(a.GetText(), url.MakeRelativeUri(a.Attributes["href"].Value));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, string bookId, string title, Uri url) {
        if (Config.Options.NoChapters) {
            return [];
        }
        
        var chapter = new Chapter {
            Title = title
        };
        
        var pages = int.Parse(doc.GetTextBySelector("span.issueNumber").Split("/").Last());
        var sb = new StringBuilder();
        for (var i = 1; i <= pages; i++) {
            Config.Logger.LogInformation($"Получаю страницу {i}/{pages}");
            var response = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri($"{bookId}/{i}"));
            var img = response.QuerySelector("#mainImage");
            var src = url.MakeRelativeUri(img.Attributes["src"].Value);
            sb.Append($"<img src='{src}'/>");
        }

        var chapterDoc = sb.AsHtmlDoc();
        chapter.Images = await GetImages(chapterDoc, url);
        chapter.Content = chapterDoc.DocumentNode.InnerHtml;

        return new [] { chapter };
    }
}