using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class LibstGetter : GetterBase {
    public LibstGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://libst.ru/");

    protected override string GetId(Uri url) => url.GetQueryParameter("BookID") ?? base.GetId(url);

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = SystemUrl.MakeRelativeUri($"/Detail/BookView/{bookId}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        
        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc),
            Title = doc.GetTextBySelector("h1"),
            Author = GetAuthor(doc, url),
        };
            
        return book;
    }

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl);
        var token = doc.QuerySelector("input[name=__RequestVerificationToken]").Attributes["value"].Value;

        var response = await Config.Client.PostAsync(SystemUrl.MakeRelativeUri("/Account/Login?ReturnUrl=/"), GenerateAuthData(token));
        doc = await response.Content.ReadAsStringAsync().ContinueWith(t => t.Result.AsHtmlDoc());
        var error = doc.GetTextBySelector("div.validation-summary-errors");
        if (string.IsNullOrEmpty(error)) {
            Console.WriteLine("Авторизация прошла успешно");
        } else {
            throw new Exception($"Не удалось авторизоваться. {error}");
        }
    }
    
    private FormUrlEncodedContent GenerateAuthData(string token) {
        var data = new Dictionary<string, string> {
            ["__RequestVerificationToken"] = token,
            ["username"] = Config.Options.Login,
            ["password"] = Config.Options.Password
        };

        return new FormUrlEncodedContent(data);
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc) {
        var result = new List<Chapter>();

        foreach (var bookChapter in GetToc(doc)) {
            Console.WriteLine($"Загружаю главу {bookChapter.Title.CoverQuotes()}");
            
            var chapter = new Chapter {
                Title = bookChapter.Title
            };

            var chapterDoc = await GetChapter(bookChapter);
            if (chapterDoc != default) {
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(IdChapter bookChapter) {
        if (string.IsNullOrWhiteSpace(bookChapter.Id)) {
            return default;
        }
        
        var sb = new StringBuilder();
        
        for (var i = 1;; i++) {
            var response = await Config.Client.GetAsync(SystemUrl.MakeRelativeUri($"/Detail/GetChapterTextForCanvas?ChapID={bookChapter.Id}&k=0&page={i}"));
            if (response.StatusCode != HttpStatusCode.OK) {
                break;
            }

            var data = await response.Content.ReadAsStringAsync().ContinueWith(t => t.Result.Deserialize<string[]>());
            sb.AppendJoin(string.Empty, data.Select(r => r.Trim().CoverTag("p")));
        }

        return sb.AsHtmlDoc();
    }

    private IEnumerable<IdChapter> GetToc(HtmlDocument doc) {
        var result = new List<IdChapter>();

        foreach (var span in doc.QuerySelectorAll("span.mt-action-author")) {
            var onclick = span.Attributes["onclick"]?.Value;
            if (string.IsNullOrWhiteSpace(onclick)) {
                result.Add(new IdChapter(string.Empty, span.GetText()));
            } else {
                var id = Regex.Match(onclick, @"ChapID=(?<id>\d+)").Groups["id"].Value;
                result.Add(new IdChapter(id, span.GetText()));
            }
        }

        return SliceToc(result);
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("img.coverBook")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
    
    private static Author GetAuthor(HtmlDocument doc, Uri uri) {
        var a = doc.QuerySelector("h2.author_name a");
        return new Author(a.GetText(), uri.MakeRelativeUri(a.Attributes["href"].Value));
    }
}