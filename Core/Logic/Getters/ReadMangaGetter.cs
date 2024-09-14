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
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class ReadMangaGetter : GetterBase {
    public ReadMangaGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://readmanga.live/");

    protected override string GetId(Uri url) => url.GetSegment(1);
    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri(GetId(url));
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("span.name"),
            Author = new Author("ReadManga"),
            Annotation = doc.QuerySelector("div.manga-description").InnerHtml
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }
            
        foreach (var urlChapter in GetToc(doc, url)) {
            Config.Logger.LogInformation($"Загружаю главу {urlChapter.Title.CoverQuotes()}");
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
            .QuerySelectorAll("td.item-title a.chapter-link")
            .Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.GetText().ReplaceNewLine()))
            .Reverse()
            .ToList();
        return SliceToc(result, c => c.Title);
    }

    private async Task<HtmlDocument> GetChapter(UrlChapter urlChapter) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url.AppendQueryParameter("mtr", "1"));
        if (doc.QuerySelector("div.buy-button") != default) {
            return default;
        }
        
        var json = Regex.Match(doc.ParsedText, @"readerInit(.*?)(?<data>\[\[(.*?)]]),").Groups["data"].Value.Replace("'", "\"").Deserialize<List<JsonElement[]>>();

        var sb = new StringBuilder();
        
        foreach (var elem in json) {
            var imageUri = elem[0].GetString().AsUri().MakeRelativeUri(elem[2].GetString());
            // Это не костыль. На сайте также
            if (imageUri.Host == "one-way.work") {
                imageUri = imageUri.MakeRelativeUri(imageUri.AbsolutePath);
            }
            sb.Append($"<img src='{imageUri}'/>");
        }
        
        return sb.AsHtmlDoc();
    }

    private Task<TempFile> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.picture-fotorama img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
}