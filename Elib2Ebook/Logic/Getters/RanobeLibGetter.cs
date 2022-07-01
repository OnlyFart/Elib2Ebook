using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.RanobeLib;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Elib2Ebook.Extensions;

namespace Elib2Ebook.Logic.Getters; 

public class RanobeLibGetter : GetterBase {
    public RanobeLibGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ranobelib.me");
    
    // cloudflare :(
    private string _host;

    protected override string GetId(Uri url) {
        return url.Segments[1].Trim('/');
    }

    public override async Task Init() {
        await base.Init();
        
        var response = await Config.Client.GetAsync("https://ranobelib.me/");
        if (response.StatusCode == HttpStatusCode.OK) {
            Console.WriteLine("Основной домен https://ranobelib.me/ доступен. Работаю через него");
            _host = "ranobelib.me";
            return;
        }
        
        Console.WriteLine("Основной домен https://ranobelib.me/ не доступен. Работаю через https://staticlib.me/");
        _host = "staticlib.me";
    }

    /// <summary>
    /// Авторизация в системе
    /// </summary>
    /// <exception cref="Exception"></exception>
    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }
        
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://{_host}/"));
        var token = doc.QuerySelector("meta[name=_token]")?.Attributes["content"]?.Value;
        using var post = await Config.Client.PostAsync($"https://{_host}/login", GenerateAuthData(token));
    }

    private MultipartFormDataContent GenerateAuthData(string token) {
        return new() {
            {new StringContent(token), "_token"},
            {new StringContent(Config.Options.Login), "email"},
            {new StringContent(Config.Options.Password), "password"},
            {new StringContent("on"), "remember"}
        };
    }

    public override async Task<Book> Get(Uri url) {
        var bidId = url.GetQueryParameter("bid");
        url = new Uri($"https://{_host}/{GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var data = GetData(doc);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(data, url, bidId),
            Title = doc.QuerySelector("meta[property=og:title]").Attributes["content"].Value.Trim(),
            Author = GetAuthor(doc, url)
        };
            
        return book;
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        foreach (var div in doc.QuerySelectorAll("div.media-info-list__item")) {
            var title = div.GetTextBySelector("div.media-info-list__title");
            var value = div.QuerySelector("div.media-info-list__value a");
            if (title == "Автор" && value != default) {
                return new Author(value.GetText(), new Uri(url, value.Attributes["href"].Value));
            }
        }

        return new Author("RanobeLib");
    }

    private static WindowData GetData(HtmlDocument doc) {
        var match = new Regex("window.__DATA__ = (?<data>{.*}).*window._SITE_COLOR_", RegexOptions.Compiled | RegexOptions.Singleline).Match(doc.Text).Groups["data"].Value;
        var windowData = match.Deserialize<WindowData>();
        windowData.RanobeLibChapters.List.Reverse();
        return windowData;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(WindowData data, Uri url, string bidId) {
        var result = new List<Chapter>();
        var branchId = string.IsNullOrWhiteSpace(bidId)
            ? data.RanobeLibChapters.List
                .GroupBy(c => c.BranchId)
                .MaxBy(c => c.Count())!
                .Key
            : int.Parse(bidId);

        foreach (var ranobeChapter in data.RanobeLibChapters.List.Where(c => c.BranchId == branchId)) {
            Console.WriteLine($"Загружаю главу {ranobeChapter.GetName()}");
            var chapter = new Chapter();
            var chapterDoc = await GetChapter(ranobeChapter.GetUri(url));
            chapter.Images = await GetImages(chapterDoc, ranobeChapter.GetUri(url));
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = ranobeChapter.GetName();

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri url) {
        var chapterDoc = await Config.Client.GetHtmlDocWithTriesAsync(url.ReplaceHost(_host));
        var header = chapterDoc.QuerySelector("h2.page__title");
        if (header != default && header.GetText() == "Регистрация") {
            throw new Exception("Произведение доступно только зарегистрированным пользователям. Добавьте в параметры вызова свои логин и пароль");
        }
        
        
        return chapterDoc.QuerySelector("div.reader-container").InnerHtml.AsHtmlDoc();
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("meta[property=og:image]").Attributes["content"].Value.Trim();
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri($"https://{_host}" + new Uri(uri, imagePath).AbsolutePath)) : Task.FromResult(default(Image));
    }
}