using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters;

public class LitLifeGetter : GetterBase {
    public LitLifeGetter(BookGetterConfig config) : base(config) { }

    protected override Uri SystemUrl => new("https://litlife.club/");

    protected override string GetId(Uri url) {
        return url.GetSegment(2);
    }

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl);
        var token = doc.QuerySelector("#login_form input[name=_token]").Attributes["value"].Value;
        
        doc = await Config.Client.PostHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri("/login"), GenerateAuthData(token));
        var alert = doc.QuerySelector("div.alert-danger");
        if (alert == default) {
            Console.WriteLine("Успешно авторизовались");
        } else {
            throw new Exception($"Не удалось авторизоваться. {alert.GetText()}");
        }
    }

    private FormUrlEncodedContent GenerateAuthData(string token) {
        var payload = new Dictionary<string, string> {
            { "_token", token },
            { "login", Config.Options.Login },
            { "login_password", Config.Options.Password },
            { "remember", "on" },
        };

        return new FormUrlEncodedContent(payload);
    }

    public override async Task<Book> Get(Uri url) {
        var id = GetId(url);
        url = SystemUrl.MakeRelativeUri("/books/" + id);
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var noAccess = doc.QuerySelector("p.no_access_text");
        if (noAccess != default) {
            throw new Exception("Книна не доступна для чтения.");
        }

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(id),
            Title = doc.GetTextBySelector("div.card-header h2[itemprop=name]"),
            Author = GetAuthor(doc),
            Seria = GetSeria(doc),
            Annotation = doc.QuerySelector("#annotation")?.InnerHtml
        };
            
        return book;
    }
    
    private Seria GetSeria(HtmlDocument doc) {
        var a = doc.QuerySelector("div.card-body a.sequence");
        if (a != default) {
            return new Seria {
                Name = a.GetText(),
                Url = SystemUrl.MakeRelativeUri(a.Attributes["href"].Value)
            };
        }

        return default;
    }

    private Author GetAuthor(HtmlDocument doc) {
        var a = doc.QuerySelector("div.card-body h3[itemprop=author] a.author");
        return new Author(a.GetText(), SystemUrl.MakeRelativeUri(a.Attributes["href"].Value));
    }
    
    private async Task<IEnumerable<UrlChapter>> GetToc(string id) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri($"/books/{id}/sections/list_go_to"));

        var result = doc.QuerySelectorAll("ul a")
            .Select(a => new UrlChapter(SystemUrl.MakeRelativeUri(a.Attributes["href"].Value), a.RemoveNodes("span").GetText().ReplaceNewLine()))
            .ToList();

        return SliceToc(result);
    }

    private async Task<IEnumerable<Chapter>> FillChapters(string id) {
        var result = new List<Chapter>();
            
        foreach (var urlChapter in await GetToc(id)) {
            Console.WriteLine($"Загружаю главу {urlChapter.Title.CoverQuotes()}");
   
            var chapter = new Chapter {
                Title = urlChapter.Title
            };

            if (urlChapter.Url != default) {
                var chapterDoc = await GetChapter(urlChapter);
                chapter.Images = await GetImages(chapterDoc, urlChapter.Url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(UrlChapter urlChapter) {
        var result = new StringBuilder();
        
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url);
        result.Append(doc.QuerySelector("div.book_text").RemoveNodes("#read_online_ad, h4").InnerHtml);

        var pages = doc.QuerySelectorAll("div.card-body ul.pagination a.page-link")
            .Where(a => int.TryParse(a.GetText(), out _))
            .Select(a => int.Parse(a.GetText()))
            .ToList();
        
        if (pages.Count > 0) {
            for (var i = 2; i <= pages.Max(); i++) {
                doc = await Config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url.AppendQueryParameter("page", i));
                result.Append(doc.QuerySelector("div.book_text").RemoveNodes("#read_online_ad, h4").InnerHtml);
            }
        }
        
        return result.AsHtmlDoc();
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.cover img")?.Attributes["data-src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}