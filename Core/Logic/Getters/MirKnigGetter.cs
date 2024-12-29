using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class MirKnigGetter(BookGetterConfig config) : GetterBase(config) {
    protected override Uri SystemUrl => new("https://mir-knig.com/");

    protected override string GetId(Uri url) => base.GetId(url).Split('_').Last().Split('-').First();

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = SystemUrl.MakeRelativeUri($"/view_{bookId}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var title = doc.GetTextBySelector("h1.heading");
        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(bookId, title),
            Title = title,
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div.desc")?.InnerHtml
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(string bookId, string title) {
        if (Config.Options.NoChapters) {
            return [];
        }
        
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri($"/read_{bookId}-1"));
        var pages = int.Parse(doc.QuerySelectorAll("select.allp option").Last().Attributes["value"].Value);

        var sb = new StringBuilder();
        for (var i = 1; i <= pages; i++) {
            Config.Logger.LogInformation($"Получаю страницу {i}/{pages}");
            doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri($"/read_{bookId}-{i}"));
            sb.Append(doc.QuerySelector("div.text-block").InnerHtml.HtmlDecode());
        }

        doc = sb.AsHtmlDoc();
        var chapter = new Chapter {
            Title = title,
            Images = await GetImages(doc, SystemUrl),
            Content = doc.DocumentNode.InnerHtml
        };

        return new []{chapter};
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("a.owner");
        return new Author(a.GetText(), url.MakeRelativeUri(a.Attributes["href"].Value));
    }
        
    private Task<TempFile> GetCover(HtmlDocument doc, Uri bookUri) {
        var thumb = doc.QuerySelector("div.cover div.thumb");
        var imagePath = string.Empty;
        if (thumb != default) {
            var style = thumb.Attributes["style"]?.Value;
            if (!string.IsNullOrWhiteSpace(style)) {
                var match = Regex.Match(style, @"url\((?<url>.*?)\)");
                if (match.Success) {
                    imagePath = match.Groups["url"].Value;
                }
            }
        }
        
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
    
    
}