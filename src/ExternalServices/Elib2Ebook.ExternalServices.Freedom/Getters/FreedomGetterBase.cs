using Elib2Ebook.Domain.Book;
using Elib2Ebook.Domain.Common;
using Elib2Ebook.DomainServices.Configs;
using Elib2Ebook.DomainServices.Extensions;
using Elib2Ebook.DomainServices.Getters;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Elib2Ebook.ExternalServices.Freedom.Getters;

public abstract class FreedomGetterBase(BookGetterConfig config) : GetterBase(config)
{
    public override async Task<Book> Get(Uri url)
    {
        url = await GetMainUrl(url);
        url = SystemUrl.MakeRelativeUri($"/ranobe/{GetId(url)}/");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url)
        {
            Cover = await GetCover(doc, url), Chapters = await FillChapters(doc, url), Title = doc.GetTextBySelector("h1.entry-title"), Author = GetAuthor()
        };

        return book;
    }

    private async Task<Uri> GetMainUrl(Uri url)
    {
        if (url.GetSegment(1) == "ranobe")
        {
            return url;
        }

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        return url.MakeRelativeUri(doc.QuerySelector("div.bun2 a").Attributes["href"].Value);
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url)
    {
        var result = doc
            .QuerySelectorAll("div.li-col1-ranobe > a")
            .Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.GetText()))
            .Reverse()
            .ToList();

        return SliceToc(result, c => c.Title);
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri uri)
    {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters)
        {
            return result;
        }

        foreach (var urlChapter in GetToc(doc, uri))
        {
            var chapter = new Chapter
            {
                Title = urlChapter.Title
            };

            Config.Logger.LogInformation($"Загружаю главу {urlChapter.Title.CoverQuotes()}");

            var chapterDoc = await GetChapter(urlChapter.Url);
            if (chapterDoc != null)
            {
                chapter.Images = await GetImages(chapterDoc, urlChapter.Url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri url)
    {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        var content = doc.QuerySelector("div.entry-content");
        var notice = content.QuerySelector("div.single-notice");
        return notice?.GetText() == "Для чтения купите главу." ?
            null :
            content.InnerHtml.AsHtmlDoc().RemoveNodes("div[class*=adv]");
    }

    private static Author GetAuthor()
    {
        return new Author("Ifreedom");
    }

    private Task<TempFile> GetCover(HtmlDocument doc, Uri uri)
    {
        var imagePath = doc.QuerySelector("div.img-ranobe img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
}
