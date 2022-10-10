using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Dreame;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class DreameGetter : GetterBase {
    public DreameGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://dreame.com/");
    
    private Uri _apiUrl => new($"https://wap-api.{SystemUrl.Host}/");

    protected override string GetId(Uri url) => url.GetSegment(2).Split("-")[0];

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = $"https://www.dreame.com/story/{bookId}".AsUri();
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        var data = GetNextData<DreameNovel>(doc, "novel");
        
        var book = new Book(url) {
            Cover = await GetCover(data.Cover),
            Chapters = await FillChapters(url, bookId),
            Title = data.Name,
            Author = GetAuthor(doc, data),
            Annotation = data.Description,
            Lang = data.Language
        };
            
        return book;
    }
    
    private static T GetNextData<T>(HtmlDocument doc, string node) {
        var json = doc.QuerySelector("#__NEXT_DATA__").InnerText;
        return JsonDocument.Parse(json)
            .RootElement.GetProperty("props")
            .GetProperty("pageProps")
            .GetProperty("novelInfo")
            .GetProperty(node)
            .GetRawText()
            .Deserialize<T>();
    }
    
    private async Task<IEnumerable<Chapter>> FillChapters(Uri url, string bookId) {
        var result = new List<Chapter>();

        foreach (var urlChapter in await GetToc(bookId)) {
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

    private async Task<HtmlDocument> GetChapter(DreameChapter urlChapter) {
        var response = await Config.Client.GetFromJsonWithTriesAsync<DreameApiResponse<DreameChapter>>(_apiUrl.MakeRelativeUri($"/novel/getChapter?channel=dreamepmian-173&product=1&osType=2&cid={urlChapter.Id}&systemFlag=android&port=web"));
        return Encoding.UTF8.GetString(Convert.FromBase64String(response.Data.Content)).AsHtmlDoc();
    }

    private async Task<IEnumerable<DreameChapter>> GetToc(string bookId) {
        var response = await Config.Client.GetFromJsonWithTriesAsync<DreameApiResponse<DreameCatalog>>(_apiUrl.MakeRelativeUri($"novel/getcatelog?channel=dreamepmian-173&product=1&osType=2&nid={bookId}"));
        return SliceToc(response.Data.Pager.ChapterList);
    }

    private Author GetAuthor(HtmlDocument doc, DreameNovel data) {
        var a = doc.QuerySelector("a a[class*=story_author-name]");
        return a != default ? 
            new Author(data.AuthorName, SystemUrl.MakeRelativeUri(a.Attributes["href"].Value)) : 
            new Author(data.AuthorName);
    }

    private Task<Image> GetCover(string url) {
        return !string.IsNullOrWhiteSpace(url) ? SaveImage(url.AsUri()) : Task.FromResult(default(Image));
    }
}