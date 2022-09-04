using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.HotNovelPub;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters.HotNovelPub; 

public abstract class HotNovelPubGetterBase : GetterBase {
    public HotNovelPubGetterBase(BookGetterConfig config) : base(config) { }
    protected abstract string Lang { get; }

    public override Task Init() {
        base.Init();
        Config.Client.DefaultRequestHeaders.Add("lang", Lang);
        Config.Client.DefaultRequestHeaders.Add("Priority", "u=3, i");
        return Task.CompletedTask;
    }

    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);
        var bookApi = await GetBook(GetId(url));

        var book = new Book(url) {
            Cover = await GetCover(bookApi.Book),
            Chapters = await FillChapters(bookApi.Chapters),
            Title = bookApi.Book.Name,
            Author = GetAuthor(bookApi.Book),
            Annotation = bookApi.Book.Description,
            Lang = Lang
        };

        return book;
    }

    private async Task<Uri> GetMainUrl(Uri url) {
        if (GetId(url).StartsWith("chapter-")) {
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
            var a = doc.QuerySelectorAll("li.breadcrumb-item")[1].QuerySelector("a[href]");
            url = new Uri(SystemUrl, a.Attributes["href"].Value);
        }
        
        return url;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(ICollection<HotNovelPubChapter> toc) {
        var chapters = new List<Chapter>();
        foreach (var ezChapter in SliceToc(toc)) {
            var title = ezChapter.Title.Trim();
            Console.WriteLine($"Загружаю главу {title.CoverQuotes()}");
            
            var chapter = new Chapter {
                Title = title
            };

            var chapterDoc = await GetChapter(ezChapter);
            chapter.Images = await GetImages(chapterDoc, SystemUrl);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;

            chapters.Add(chapter);
        }
            
        return chapters;
    }

    private async Task<HtmlDocument> GetChapter(HotNovelPubChapter ezChapter) {
        var doc = Config.Client.GetHtmlDocWithTriesAsync(new Uri(SystemUrl, ezChapter.Slug));
        var additional = Config.Client.GetFromJsonAsync<HotNovelPubApiResponse<string[]>>(new Uri($"https://{SystemUrl.Host}/server/getContent?slug={ezChapter.Slug}"));
        
        const string watermark = ".copy right hot novel pub";
        var sb = new StringBuilder((await doc).QuerySelector("#content-item").InnerHtml.HtmlDecode().CleanInvalidXmlChars().Replace(watermark, string.Empty));

        
        foreach (var row in (await additional)!.Data) {
            foreach (var p in row.Split("\n", StringSplitOptions.RemoveEmptyEntries)) {
                sb.Append(p.HtmlDecode().CleanInvalidXmlChars().Replace(watermark, string.Empty).Trim().CoverTag("p"));
            }
        }

        return sb.AsHtmlDoc();
    }

    private Author GetAuthor(HotNovelPubBook book) {
        return new Author(book.Authorize.Name, new Uri(SystemUrl, book.Authorize.Slug));
    }

    private Task<Image> GetCover(HotNovelPubBook book) {
        return !string.IsNullOrWhiteSpace(book.Image) ? GetImage(new Uri(SystemUrl, book.Image)) : Task.FromResult(default(Image));
    }

    private async Task<HotNovelPubBookResponse> GetBook(string id) {
        var response = await Config.Client.GetFromJsonAsync<HotNovelPubApiResponse<HotNovelPubBookResponse>>(new Uri($"https://api.{SystemUrl.Host}/book/{id}"));
        return response!.Data;
    }
}