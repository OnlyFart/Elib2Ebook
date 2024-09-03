using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Core.Logic.Getters; 

public class DesuGetter : GetterBase {
    public DesuGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://desu.me/");

    protected override string GetId(Uri url) {
        return url.GetSegment(2);
    }

    public override async Task Init() {
        await base.Init();
        Config.Client.DefaultRequestHeaders.Add("Referer", SystemUrl.ToString());
    }

    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri($"/manga/{GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc),
            Title = GetTitle(doc),
            Author = GetAuthor(doc),
            Annotation = doc.QuerySelector("#description div.prgrph")?.InnerHtml
        };
            
        return book;
    }

    private Author GetAuthor(HtmlDocument doc) {
        var a = doc.QuerySelector("ul.translators a");
        return a == default ? new Author("Desu") : new Author(a.GetText(), SystemUrl.MakeRelativeUri(a.Attributes["href"].Value));
    }

    private static string GetTitle(HtmlDocument doc) {
        return doc.GetTextBySelector("h1 span.rus-name") ?? doc.GetTextBySelector("h1 span.name");
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc) {
        var result = new List<Chapter>();
            
        foreach (var urlChapter in GetToc(doc)) {
            Console.WriteLine($"Загружаю главу {urlChapter.Title.CoverQuotes()}");
            var chapter = new Chapter {
                Title = urlChapter.Title
            };

            var chapterDoc = await GetChapter(urlChapter);

            if (chapterDoc != default) {
                chapter.Images = await GetImages(chapterDoc, SystemUrl);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }
            
            result.Add(chapter);
        }

        return result;
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc) {
        var result = doc
            .QuerySelectorAll("ul.chlist a[href]")
            .Select(a => new UrlChapter(SystemUrl.MakeRelativeUri(a.Attributes["href"].Value), a.GetText().ReplaceNewLine()))
            .Reverse()
            .ToList();

        return SliceToc(result);
    }

    private async Task<HtmlDocument> GetChapter(UrlChapter urlChapter) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url.AppendQueryParameter("mtr", "1"));
        var images = Regex.Match(doc.ParsedText, @"Reader\.init(.*?)(?<data>\[\[(.*?)\]\]),", RegexOptions.Singleline).Groups["data"].Value.Replace("'", "\"").Deserialize<List<JsonElement[]>>();
        var dir = Regex.Match(doc.ParsedText, "dir: \"(?<data>(.*?))\"").Groups["data"].Value;

        var sb = new StringBuilder();
        
        foreach (var elem in images) {
            var imageUri = SystemUrl.MakeRelativeUri(dir).MakeRelativeUri(elem[0].GetString());
            sb.Append($"<img src='{imageUri}'/>");
        }
        
        return sb.AsHtmlDoc();
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.c-poster img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}