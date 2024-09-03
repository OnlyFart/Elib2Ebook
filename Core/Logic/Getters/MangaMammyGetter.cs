using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Core.Logic.Getters;

public class MangaMammyGetter : GetterBase {
    public MangaMammyGetter(BookGetterConfig config) : base(config) { }
    
    protected override Uri SystemUrl => new("https://mangamammy.ru/");
    
    protected override string GetId(Uri url) => url.GetSegment(2);
    
    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri("/manga/" + GetId(url));
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1"),
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div.manga-excerpt").InnerHtml
        };
            
        return book;
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("div.author-content a");
        return a == default ? new Author("MangaMammy") : new Author(a.GetText(), url.MakeRelativeUri(a.Attributes["href"].Value));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();
            
        foreach (var urlChapter in GetToc(doc, url)) {
            Console.WriteLine($"Загружаю главу {urlChapter.Title.CoverQuotes()}");
            var chapter = new Chapter {
                Title = urlChapter.Title
            };

            var chapterDoc = await GetChapter(urlChapter);

            if (chapterDoc != default) {
                chapter.Images = await GetImages(chapterDoc, url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }
            
            result.Add(chapter);
        }

        return result;
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        var result = doc
            .QuerySelectorAll("ul.sub-chap-list li.wp-manga-chapter > a")
            .Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.GetText().ReplaceNewLine()))
            .Reverse()
            .ToList();
        
        return SliceToc(result);
    }

    private async Task<HtmlDocument> GetChapter(UrlChapter urlChapter) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url);
        var json = Regex.Match(doc.ParsedText, @"chapter_preloaded_images = (?<data>\[(.*)\]),").Groups["data"].Value.Deserialize<JsonArray>();

        var sb = new StringBuilder();

        foreach (var elem in json) {
            switch (elem) {
                case JsonArray images: {
                    foreach (var image in images) {
                        sb.Append($"<img src='{image.ToString()}'/>");
                    }

                    break;
                }
                default:
                    sb.Append($"<img src='{elem.ToString()}'/>");
                    break;
            }
        }

        return sb.AsHtmlDoc();
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.summary_image img")?.Attributes["data-lazy-src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}