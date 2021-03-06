using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.RanobeHub;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class RanobeHubGetter : GetterBase {
    public RanobeHubGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ranobehub.org");
    public override async Task<Book> Get(Uri url) {
        url = new Uri($"https://ranobehub.org/ranobe/{GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1.header"),
            Author = new Author("RanobeHub")
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();

        foreach (var ranobeChapter in await GetToc(doc)) {
            Console.WriteLine($"Загружаю главу {ranobeChapter.Name}");
            var chapter = new Chapter();
            var chapterDoc = await GetChapter(ranobeChapter.Url);
            chapter.Images = await GetImages(chapterDoc, url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = ranobeChapter.Name;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(string url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(new Uri(url));
        while (doc.QuerySelector("div[data-callback=correctCaptcha]") != null) {
            Console.WriteLine($"Обнаружена каптча. Перейдите по ссылке {url}, введите каптчу и нажмите Enter...");
            Console.Read();
            doc = await Config.Client.GetHtmlDocWithTriesAsync(new Uri(url));
        }
        
        var result = doc.QuerySelector("div.container[data-container]").RemoveNodes("div.title-wrapper, div.ads-desktop, div.tablet").InnerHtml.AsHtmlDoc();
        
        foreach (var img in result.QuerySelectorAll("img")) {
            var id = img.Attributes["data-media-id"]?.Value;
            if (string.IsNullOrWhiteSpace(id)) {
                continue;
            }
            
            img.Attributes["src"].Value = $"/api/media/{id}";
        }
        
        return result;
    }

    private async Task<IEnumerable<RanobeHubChapter>> GetToc(HtmlDocument doc) {
        var internalId = doc.QuerySelector("html[data-id]").Attributes["data-id"].Value;
        var response = await Config.Client.GetFromJsonAsync<RanobeHubApiResponse>($"https://ranobehub.org/api/ranobe/{internalId}/contents");
        return response?.Volumes.SelectMany(t => t.Chapters);
    }
    
    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.poster-slider img")?.Attributes["data-src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
    }
}