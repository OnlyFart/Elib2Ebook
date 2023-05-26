using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Common;
using Elib2Ebook.Types.Freedlit;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class FreedlitGetter : GetterBase{
    public FreedlitGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://freedlit.space/");

    protected override string GetId(Uri url) {
        return url.GetSegment(2);
    }

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl);
        var token = doc.QuerySelector("meta[name=csrf-token]").Attributes["content"].Value;
        using var post = await Config.Client.PostAsync(SystemUrl.MakeRelativeUri("/login-modal"), GenerateAuthData(token));
        var response = await post.Content.ReadFromJsonAsync<FreedlitAuthResponse>();
        if (response.Errors != default) {
            Console.WriteLine("Успешно авторизовались");
        } else {
            var errors = response.Errors.Password.Aggregate(response.Errors.Email, (current, error) => current + Environment.NewLine + error);
            throw new Exception($"Не удалось авторизоваться. {errors}");
        }
    }

    private MultipartFormDataContent GenerateAuthData(string token) {
        return new() {
            {new StringContent(token), "_token"},
            {new StringContent(Config.Options.Login), "email"},
            {new StringContent(Config.Options.Password), "password"},
        };
    }

    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri($"/book/{GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        
        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc),
            Title = doc.GetTextBySelector(".book-info h4"),
            Author = GetAuthor(doc),
            Annotation = doc.QuerySelector("#nav-home")?.InnerHtml
        };
            
        return book;
    }

    private Author GetAuthor(HtmlDocument doc) {
        var a = doc.QuerySelector("a[href*=/p/]");
        var href = a?.Attributes["href"]?.Value;
        return a == default ? 
            new Author("Freedlit") : 
            new Author(a.GetText(), SystemUrl.MakeRelativeUri(href));
    }
    
    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc) {
        var result = doc
            .QuerySelectorAll("#nav-contents div.chapter-block a")
            .Select(a => new UrlChapter(SystemUrl.MakeRelativeUri(a.Attributes["href"].Value), a.GetText()))
            .ToList();
        
        return SliceToc(result);
    }
    
    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc) {
        var result = new List<Chapter>();

        foreach (var urlChapter in GetToc(doc)) {
            var chapter = new Chapter {
                Title = urlChapter.Title
            };

            Console.WriteLine($"Загружаю главу {urlChapter.Title.CoverQuotes()}");

            var chapterDoc = await GetChapter(urlChapter.Url);
            if (chapterDoc != default) {
                chapter.Images = await GetImages(chapterDoc, urlChapter.Url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }
            
            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        return doc.QuerySelector("div.chapter").RemoveNodes("h2").InnerHtml.AsHtmlDoc();
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.book-cover img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}