using System.Net.Http.Json;
using System.Text;
using Elib2Ebook.Domain.Book;
using Elib2Ebook.Domain.Common;
using Elib2Ebook.DomainServices.Configs;
using Elib2Ebook.DomainServices.Extensions;
using Elib2Ebook.DomainServices.Getters;
using Elib2Ebook.ExternalServices.HotNovelPub.Types;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Elib2Ebook.ExternalServices.HotNovelPub.Getters;

public abstract class HotNovelPubGetterBase(BookGetterConfig config) : GetterBase(config)
{
    protected abstract string Lang { get; }

    private Uri _apiUrl => new($"https://api.{SystemUrl.Host}/");

    public override Task Init()
    {
        base.Init();
        Config.Client.DefaultRequestHeaders.Add("lang", Lang);
        Config.Client.DefaultRequestHeaders.Add("Priority", "u=3, i");
        return Task.CompletedTask;
    }

    public override async Task<Book> Get(Uri url)
    {
        url = await GetMainUrl(url);
        var bookApi = await GetBook(GetId(url));

        var book = new Book(url)
        {
            Cover = await GetCover(bookApi.Book),
            Chapters = await FillChapters(bookApi.Chapters),
            Title = bookApi.Book.Name,
            Author = GetAuthor(bookApi.Book),
            Annotation = bookApi.Book.Description,
            Lang = Lang
        };

        return book;
    }

    private async Task<Uri> GetMainUrl(Uri url)
    {
        if (GetId(url).StartsWith("chapter-"))
        {
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
            var a = doc.QuerySelectorAll("li.breadcrumb-item")[1].QuerySelector("a[href]");
            url = SystemUrl.MakeRelativeUri(a.Attributes["href"].Value);
        }

        return url;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(ICollection<HotNovelPubChapter> toc)
    {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters)
        {
            return result;
        }

        foreach (var ezChapter in SliceToc(toc, c => c.Title))
        {
            var title = ezChapter.Title.Trim();
            Config.Logger.LogInformation($"Загружаю главу {title.CoverQuotes()}");

            var chapter = new Chapter
            {
                Title = title
            };

            var chapterDoc = await GetChapter(ezChapter);
            chapter.Images = await GetImages(chapterDoc, SystemUrl);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(HotNovelPubChapter ezChapter)
    {
        var doc = Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri(ezChapter.Slug));
        var additional = Config.Client.GetFromJsonAsync<HotNovelPubApiResponse<string[]>>(SystemUrl.MakeRelativeUri($"/server/getContent?slug={ezChapter.Slug}"));

        const string watermark = ".copy right hot novel pub";
        var sb = new StringBuilder((await doc).QuerySelector("#content-item").InnerHtml.HtmlDecode().CleanInvalidXmlChars().Replace(watermark, string.Empty));

        foreach (var row in (await additional)!.Data)
        {
            foreach (var p in row.Split("\n", StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Append(p.HtmlDecode().CleanInvalidXmlChars().Replace(watermark, string.Empty).Trim().CoverTag("p"));
            }
        }

        return sb.AsHtmlDoc();
    }

    private Author GetAuthor(HotNovelPubBook book)
    {
        return new Author(book.Authorize.Name, SystemUrl.MakeRelativeUri(book.Authorize.Slug));
    }

    private Task<TempFile> GetCover(HotNovelPubBook book)
    {
        return !string.IsNullOrWhiteSpace(book.Image) ? SaveImage(SystemUrl.MakeRelativeUri(book.Image)) : Task.FromResult(default(TempFile));
    }

    private async Task<HotNovelPubBookResponse> GetBook(string id)
    {
        var response = await Config.Client.GetFromJsonAsync<HotNovelPubApiResponse<HotNovelPubBookResponse>>(_apiUrl.MakeRelativeUri($"/book/{id}"));
        return response!.Data;
    }
}
