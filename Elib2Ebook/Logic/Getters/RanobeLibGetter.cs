using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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
    private const string HOST = "staticlib.me";

    protected override string GetId(Uri url) {
        return url.Segments[1].Trim('/');
    }
        
    /// <summary>
    /// Авторизация в системе
    /// </summary>
    /// <exception cref="Exception"></exception>
    private async Task Authorize(HtmlDocument doc){
        if (!_config.HasCredentials) {
            return;
        }
            
        var token = doc.QuerySelector("meta[name=_token]")?.Attributes["content"]?.Value;
        using var post = await _config.Client.PostAsync($"https://{HOST}/login", GenerateAuthData(token));
    }
        
    public MultipartFormDataContent GenerateAuthData(string token) {
        return new() {
            {new StringContent(token), "_token"},
            {new StringContent(_config.Login), "email"},
            {new StringContent(_config.Password), "password"},
            {new StringContent("on"), "remember"}
        };
    }

    public override async Task<Book> Get(Uri url) {
        Init();
        var bookId = GetId(url);
        var uri = new Uri($"https://{HOST}/{bookId}");
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(uri);
        await Authorize(doc);

        var data = GetData(doc);

        var book = new Book {
            Cover = await GetCover(doc, uri),
            Chapters = await FillChapters(data, uri),
            Title = doc.QuerySelector("meta[property=og:title]").Attributes["content"].Value.Trim(),
            Author = "RanobeLib"
        };
            
        return book;
    }

    private WindowData GetData(HtmlDocument doc) {
        var match = new Regex("window.__DATA__ = (?<data>{.*}).*window._SITE_COLOR_", RegexOptions.Compiled | RegexOptions.Singleline).Match(doc.Text).Groups["data"].Value;
        var windowData = JsonSerializer.Deserialize<WindowData>(match);
        windowData.Chapters.List.Reverse();
        return windowData;
    }

    private async Task<IEnumerable<Types.Book.Chapter>> FillChapters(WindowData data, Uri url) {
        var result = new List<Types.Book.Chapter>();
        var branchId = data.Chapters.List
            .GroupBy(c => c.BranchId)
            .MaxBy(c => c.Count())!
            .Key;

        foreach (var ranobeChapter in data.Chapters.List.Where(c => c.BranchId == branchId)) {
            Console.WriteLine($"Загружаем главу {ranobeChapter.GetName()}");
            var chapter = new Types.Book.Chapter();
            var chapterDoc = await GetChapter(ranobeChapter.GetUri(url));
            chapter.Images = await GetImages(chapterDoc, ranobeChapter.GetUri(url));
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = ranobeChapter.GetName();

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri url) {
        var chapterDoc = await _config.Client.GetHtmlDocWithTriesAsync(url);
        return chapterDoc.QuerySelector("div.reader-container").InnerHtml.AsHtmlDoc();
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("meta[property=og:image]").Attributes["content"].Value.Trim();
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri($"https://{HOST}" + new Uri(uri, imagePath).AbsolutePath)) : Task.FromResult(default(Image));
    }
}