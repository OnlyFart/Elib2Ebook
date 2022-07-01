using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Ranobe;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class RanobeGetter : GetterBase {
    public RanobeGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ранобэ.рф/");
        
    protected override string GetId(Uri url) {
        var segments = url.Segments;
        return (segments.Length == 2 ? base.GetId(url) : segments[1]).Trim('/');
    }
        
    public override async Task<Book> Get(Uri url) {
        url = new Uri($"https://ранобэ.рф/{GetId(url)}");
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
            
        foreach (var ranobeChapter in ranobeBook.Chapters.Reverse()) {
            Console.WriteLine($"Загружаю главу {ranobeChapter.Title.CoverQuotes()}");
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
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(new Uri(mainUrl, url));
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
        
    private Task<Image> GetCover(RanobeBook book, Uri bookUri) {
        var imagePath = book.Image?.Url ?? book.Images.MaxBy(t => t.Height)?.Url;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
    }
}