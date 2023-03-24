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

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = SystemUrl.MakeRelativeUri(bookId);
        var wattpadInfo = await Config.Client.GetFromJsonAsync<WattpadInfo>(SystemUrl.MakeRelativeUri($"/api/v3/stories/{bookId}"));

        var book = new Book(url) {
            Cover = await GetCover(wattpadInfo),
            Chapters = await FillChapters(wattpadInfo),
            Title = wattpadInfo.Title,
            Author = new Author(wattpadInfo.User.Name, SystemUrl.MakeRelativeUri($"/user/{wattpadInfo?.User.Name}")),
            Annotation = wattpadInfo?.Description
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(WattpadInfo wattpadInfo) {
        var result = new List<Chapter>();
            
        foreach (var group in SliceToc(wattpadInfo.Parts)) {
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

    private async Task<HtmlDocument> GetChapter(WattpadPart part) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(part.Url);
        foreach (var node in doc.QuerySelectorAll("p")) {
            node.Attributes.RemoveAll();
        }
        
        return doc;
    }

    private Task<Image> GetCover(WattpadInfo wattpadInfo) {
        return !string.IsNullOrWhiteSpace(wattpadInfo.Cover) ? SaveImage(wattpadInfo.Cover.AsUri()) : Task.FromResult(default(Image));
    }
}