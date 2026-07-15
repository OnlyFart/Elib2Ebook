using System.Text.Json;
using System.Text.RegularExpressions;
using Elib2Ebook.Domain.Book;
using Elib2Ebook.Domain.Common;
using Elib2Ebook.DomainServices.Configs;
using Elib2Ebook.DomainServices.Extensions;
using Elib2Ebook.DomainServices.Getters;
using Elib2Ebook.ExternalServices.Librebook.Types;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Elib2Ebook.ExternalServices.Librebook;

public class LibrebookGetter(BookGetterConfig config) : GetterBase(config)
{
    protected override Uri SystemUrl => new("https://librebook.me/");

    public override bool IsSameUrl(Uri url)
    {
        return url.IsSameSubDomain(SystemUrl);
    }

    protected override string GetId(Uri url)
    {
        return url.GetSegment(1);
    }

    public override async Task Init()
    {
        await base.Init();
        Config.Client.DefaultRequestHeaders.Add("Referer", SystemUrl.ToString());
    }

    public override async Task Authorize()
    {
        if (!Config.HasCredentials)
        {
            return;
        }

        var loginPage = "https://1.grouple.co/internal/auth/login?siteId=6&targetUri=%2Flogin%2FcontinueSso%3FsiteId%3D6%26targetUri%3D%252F".AsUri();
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(loginPage);

        var formUrl = loginPage.MakeRelativeUri(doc.QuerySelector("form")?.Attributes["action"]?.Value);
        doc = await Config.Client.PostHtmlDocWithTriesAsync(formUrl, GenerateAuthData());

        var noty = Regex.Match(doc.ParsedText, @"showNoty\((.*?)\)");
        var result = JsonSerializer.Deserialize<LibrebookAuthResponse>(noty.Groups[1].Value);
        if (result.Type == "success")
        {
            Config.Logger.LogInformation("Успешно авторизовались");
        }
        else
        {
            throw new Exception($"Не удалось авторизоваться. {result.Text}");
        }
    }

    private FormUrlEncodedContent GenerateAuthData()
    {
        var payload = new Dictionary<string, string>
        {
            {
                "targetUri", "/login/continueSso?siteId=6&targetUri=%2F"
            },
            {
                "username", Config.Options.Login
            },
            {
                "password", Config.Options.Password
            },
            {
                "remember_me", "true"
            },
            {
                "_remember_me_yes", string.Empty
            },
            {
                "remember_me_yes", "on"
            },
        };

        return new FormUrlEncodedContent(payload);
    }

    public override async Task<Book> Get(Uri url)
    {
        url = SystemUrl.MakeRelativeUri(GetId(url)).ReplaceHost(url.Host);
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var chapterLink = doc.QuerySelector("a.chapter-link");
        if (chapterLink == null)
        {
            throw new Exception("Книга недоступна для чтения. Возможно, нужна авторизация.");
        }

        var startUrl = url.MakeRelativeUri(chapterLink.Attributes["href"].Value);
        var book = new Book(url)
        {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(startUrl),
            Title = doc.GetTextBySelector("h1.cr-hero-names__main"),
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div.manga-description > div")?.InnerHtml
        };

        return book;
    }

    private Author GetAuthor(HtmlDocument doc, Uri bookUrl)
    {
        var a = doc.QuerySelector("span.elem_author a");
        return a == null ? new Author("Librebook") : new Author(a.GetText(), bookUrl.MakeRelativeUri(a.Attributes["href"].Value));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(Uri startUrl)
    {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters)
        {
            return result;
        }

        foreach (var urlChapter in await GetToc(startUrl))
        {
            Config.Logger.LogInformation($"Загружаю главу {urlChapter.Title.CoverQuotes()}");
            var chapter = new Chapter
            {
                Title = urlChapter.Title
            };

            var chapterDoc = await GetChapter(urlChapter);

            if (chapterDoc != null)
            {
                chapter.Images = await GetImages(chapterDoc, SystemUrl);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }

            result.Add(chapter);
        }

        return result;
    }

    private async Task<IEnumerable<UrlChapter>> GetToc(Uri startUrl)
    {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(startUrl);
        var result = doc
            .QuerySelectorAll("#chapters-list a.chapter-link")
            .Select(a => new UrlChapter(SystemUrl.MakeRelativeUri(a.Attributes["href"].Value), a.GetText().ReplaceNewLine()))
            .ToList();

        return SliceToc(result, c => c.Title);
    }

    private async Task<HtmlDocument> GetChapter(UrlChapter urlChapter)
    {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url);
        var chapter = doc.QuerySelector("div.b-chapter");
        if (chapter == null) {
            return null;
        }
        return chapter.InnerHtml.AsHtmlDoc();
    }

    private Task<TempFile> GetCover(HtmlDocument doc, Uri bookUri)
    {
        var imagePath = doc.QuerySelector("img.fotorama__img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
}
