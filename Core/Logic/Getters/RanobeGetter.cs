using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using Core.Types.Ranobe;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class RanobeGetter(BookGetterConfig config) : GetterBase(config) {
    protected override Uri SystemUrl => new("https://ранобэ.рф/");
        
    protected override string GetId(Uri url) => url.Segments.Length == 2 ? base.GetId(url) : url.GetSegment(1);

    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri(GetId(url));
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var ranobeBook = GetNextData<RanobeBook>(doc, "book");

        var book = new Book(url) {
            Cover = await GetCover(ranobeBook, url),
            Chapters = await FillChapters(ranobeBook, url),
            Title = ranobeBook.Title,
            Author = new Author(string.IsNullOrWhiteSpace(ranobeBook.Author) ? "Ranobe" : ranobeBook.Author)
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(RanobeBook ranobeBook, Uri url) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        ranobeBook.Chapters.Reverse();
        foreach (var ranobeChapter in SliceToc(ranobeBook.Chapters, c => c.Title)) {
            Config.Logger.LogInformation($"Загружаю главу {ranobeChapter.Title.CoverQuotes()}");
            var chapter = new Chapter();
            var doc = await GetChapter(url, ranobeChapter.Url);
            chapter.Images = await GetImages(doc, url);
            chapter.Content = doc.DocumentNode.InnerHtml;
            chapter.Title = ranobeChapter.Title;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri mainUrl, string url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(mainUrl.MakeRelativeUri(url));
        return GetNextData<RanobeChapter>(doc, "chapter").Content.Text.AsHtmlDoc();
    }

    private static T GetNextData<T>(HtmlDocument doc, string node) {
        var json = doc.QuerySelector("#__NEXT_DATA__").InnerText;
        return JsonDocument.Parse(json)
            .RootElement.GetProperty("props")
            .GetProperty("pageProps")
            .GetProperty(node)
            .GetRawText()
            .Deserialize<T>();
    }
        
    private Task<TempFile> GetCover(RanobeBook book, Uri bookUri) {
        var imagePath = book.Image?.Url ?? book.Images.MaxBy(t => t.Height)?.Url;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
}