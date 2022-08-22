using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.MangaLib;
using Elib2Ebook.Types.RanobeLib;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class MangaLibGetter : GetterBase {
    public MangaLibGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://mangalib.me/");
    protected override string GetId(Uri url) {
        return url.Segments[1].Trim('/');
    }
    
    /// <summary>
    /// Авторизация в системе
    /// </summary>
    /// <exception cref="Exception"></exception>
    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }
        
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://mangalib.me/"));
        var token = doc.QuerySelector("meta[name=_token]")?.Attributes["content"]?.Value;
        using var post = await Config.Client.PostAsync("https://mangalib.me/login", GenerateAuthData(token));
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
        url = new Uri($"https://mangalib.me/{GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        
        var data = GetData(doc);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(data, url),
            Title = doc.QuerySelector("meta[property=og:title]").Attributes["content"].Value.Trim(),
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div.media-description__text")?.InnerHtml
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

        return new Author("MangaLib");
    }

    private static IEnumerable<MangaLibPg> GetPg(HtmlDocument doc) {
        var match = new Regex("window.__pg = (?<data>.*);", RegexOptions.Compiled | RegexOptions.Singleline).Match(doc.QuerySelector("#pg").InnerText).Groups["data"].Value;
        var pg = match.Deserialize<MangaLibPg[]>();
        return pg.OrderBy(p => p.P);
    }
    
    private static WindowData GetData(HtmlDocument doc) {
        var match = new Regex("window.__DATA__ = (?<data>{.*}).*window._SITE_COLOR_", RegexOptions.Compiled | RegexOptions.Singleline).Match(doc.Text).Groups["data"].Value;
        var windowData = match.Deserialize<WindowData>();
        windowData.RanobeLibChapters.List.Reverse();
        return windowData;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(WindowData data, Uri url) {
        var result = new List<Chapter>();

        foreach (var ranobeChapter in SliceToc(data.RanobeLibChapters.List)) {
            Console.WriteLine($"Загружаю главу {ranobeChapter.GetName()}");
            var chapter = new Chapter();

            var chapterDoc = await GetChapter(url, ranobeChapter);
            chapter.Images = await GetImages(chapterDoc, SystemUrl);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = ranobeChapter.GetName();

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri url, RanobeLibChapter ranobeLibChapter) {
        var chapterDoc = await Config.Client.GetHtmlDocWithTriesAsync(new Uri(url + $"/v{ranobeLibChapter.ChapterVolume}/c{ranobeLibChapter.ChapterNumber}"));
        var header = chapterDoc.QuerySelector("div.auth-form-title");
        if (header != default && header.GetText() == "Авторизация") {
            throw new Exception("Произведение доступно только зарегистрированным пользователям. Добавьте в параметры вызова свои логин и пароль");
        }
        
        var pg = GetPg(chapterDoc);
        var sb = new StringBuilder();

        foreach (var p in pg) {
            var imageUrl = $"https://img3.cdnlib.link/manga/{GetId(url)}/chapters/{ranobeLibChapter.ChapterId}/{p.U}";
            sb.Append($"<img src='{imageUrl}'/>");
        }

        return sb.AsHtmlDoc();
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("meta[property=og:image]").Attributes["content"].Value.Trim();
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, imagePath)) : Task.FromResult(default(Image));
    }
}