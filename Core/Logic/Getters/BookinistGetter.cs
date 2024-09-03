using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class BookinistGetter : GetterBase {
    public BookinistGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://bookinist.pw/");
    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri($"/book/{GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("title"),
            Author = new Author("Bookinist")
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        foreach (var bookChapter in GetToc(doc, url)) {
            Config.Logger.LogInformation($"Загружаю главу {bookChapter.Title.CoverQuotes()}");
            var chapter = new Chapter {
                Title = bookChapter.Title
            };
            
            var chapterDoc = await GetChapter(bookChapter);
            chapter.Images = await GetImages(chapterDoc, url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;

            result.Add(chapter);
        }

        return result;
    }
    
    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("meta[property='og:image']")?.Attributes["content"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }

    private Task<HtmlDocument> GetChapter(UrlChapter bookChapter) {
        return Config.Client.GetHtmlDocWithTriesAsync(bookChapter.Url).ContinueWith(t => t.Result.RemoveNodes("h2"));
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        var bookId = GetId(url);
        var result = doc.QuerySelectorAll("ul.menu-toc li a").Skip(1).Select((a, i) => new UrlChapter(SystemUrl.MakeRelativeUri($"/bookpage/{bookId}/{i + 1}"), a.GetText())).ToList();
        return SliceToc(result);
    }
}