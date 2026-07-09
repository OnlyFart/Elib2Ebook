using System.Text;
using Elib2Ebook.Domain.Book;
using Elib2Ebook.Domain.Common;
using Elib2Ebook.DomainServices.Configs;
using Elib2Ebook.DomainServices.Extensions;
using Elib2Ebook.DomainServices.Getters;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Elib2Ebook.ExternalServices.OnlineKnigi;

public class OnlineKnigiGetter(BookGetterConfig config) : GetterBase(config)
{
    protected override Uri SystemUrl => new("https://online-knigi.com.ua/");

    protected override string GetId(Uri url)
        => url.GetSegment(2);

    public override async Task<Book> Get(Uri url)
    {
        var bookId = GetId(url);
        url = SystemUrl.MakeRelativeUri($"/kniga/{bookId}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var title = doc.GetTextBySelector("h1");
        var book = new Book(url)
        {
            Cover = await GetCover(doc),
            Chapters = await FillChapters(bookId, title),
            Title = title,
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div.book_description")?.InnerHtml,
        };

        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(string bookId, string title)
    {
        if (Config.Options.NoChapters)
        {
            return [];
        }

        var chapter = new Chapter();
        var sb = new StringBuilder();
        var pages = await GetPages(bookId);

        for (var i = 1; i <= pages; i++)
        {
            Config.Logger.LogInformation($"Получаю страницу {i}/{pages}");

            var uri = SystemUrl.MakeRelativeUri($"/page/{bookId}?page={i}");
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(uri);
            sb.Append(doc.QuerySelector("div.content_book").InnerHtml.HtmlDecode());
        }

        var chapterDoc = sb.AsHtmlDoc().RemoveNodes("div.adv_text");

        chapter.Title = title;
        chapter.Content = chapterDoc.DocumentNode.InnerHtml;

        return
        [
            chapter,
        ];
    }

    private async Task<int> GetPages(string bookId)
    {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri($"/page/{bookId}"));
        return int.Parse(doc.QuerySelector("li.last a").Attributes["href"].Value.AsUri().GetQueryParameter("page"));
    }

    private Task<TempFile> GetCover(HtmlDocument doc)
    {
        var imagePath = doc.QuerySelector("meta[property=og:image]")?.Attributes["content"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(SystemUrl.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }

    private static Author GetAuthor(HtmlDocument doc, Uri uri)
    {
        var a = doc.QuerySelector("div[itemprop=author] a");
        return new Author(a.GetText(), uri.MakeRelativeUri(a.Attributes["href"].Value));
    }
}
