using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Wattpad;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class WattpadGetter : GetterBase {
    public WattpadGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://wattpad.com/");

    protected override string GetId(Uri url) => base.GetId(url).Split('-')[0];

    private async Task<WattpadMeta> GetMeta(Uri url) {
        if (url.ToString().Contains("/story/")) {
            var result = new WattpadMeta();
            
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
            result.StoryId = GetId(url);
            result.BookId = doc.QuerySelector("a.read-btn").Attributes["href"].Value.Trim('/');
            result.Title = doc.GetTextBySelector("div.story-info__title");
            
            return result;
        }

        var info = await Config.Client.GetFromJsonAsync<WattpadInfo>(SystemUrl.MakeRelativeUri($"/apiv2/info?id={GetId(url)}"));
        return await GetMeta(url.MakeRelativeUri(info?.Url));
    }

    public override async Task<Book> Get(Uri url) {
        var meta = await GetMeta(url);
        var wattpadInfo = await Config.Client.GetFromJsonAsync<WattpadInfo>(SystemUrl.MakeRelativeUri($"/apiv2/info?id={meta.BookId}"));

        var book = new Book(url) {
            Cover = await GetCover(wattpadInfo),
            Chapters = await FillChapters(wattpadInfo),
            Title = meta.Title,
            Author = new Author(wattpadInfo?.Author, SystemUrl.MakeRelativeUri($"/user/{wattpadInfo?.Author}")),
            Annotation = wattpadInfo?.Description
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(WattpadInfo wattpadInfo) {
        var result = new List<Chapter>();
            
        foreach (var group in SliceToc(wattpadInfo.Group)) {
            Console.WriteLine($"Загружаю главу {group.GetTitle().CoverQuotes()}");
            var chapter = new Chapter();
                
            var chapterDoc = await GetChapter(group);
            chapter.Images = await GetImages(chapterDoc, group.Url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = group.GetTitle();

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(WattpadGroup group) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(group.Url);
        foreach (var node in doc.QuerySelectorAll("p")) {
            node.Attributes.RemoveAll();
        }
        
        return doc;
    }

    private Task<Image> GetCover(WattpadInfo wattpadInfo) {
        return !string.IsNullOrWhiteSpace(wattpadInfo.Cover) ? GetImage(wattpadInfo.Cover.AsUri()) : Task.FromResult(default(Image));
    }
}