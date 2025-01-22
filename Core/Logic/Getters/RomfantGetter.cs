using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class RomfantGetter(BookGetterConfig config) : GetterBase(config) {
    protected override Uri SystemUrl => new("https://romfant.ru");

    /// <summary>
    /// Авторизация в системе
    /// </summary>
    /// <exception cref="Exception"></exception>
    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }
        
        using var post = await Config.Client.PostAsync(SystemUrl.MakeRelativeUri("ajax/"), GenerateAuthData());
        var response = await post.Content.ReadAsStringAsync();
        if (response == "ok") {
            Config.Logger.LogInformation("Успешно авторизовались");
        } else {
            throw new Exception($"Не удалось авторизоваться. {response.AsHtmlDoc().DocumentNode.GetText()}");
        }
    }

    private MultipartFormDataContent GenerateAuthData() {
        return new() {
            {new StringContent("auth"), "action"},
            {new StringContent(Config.Options.Login), "login"},
            {new StringContent(Config.Options.Password), "pass"},
        };
    }

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = SystemUrl.MakeRelativeUri($"/book/{bookId}/");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var title = doc.GetTextBySelector("h1");
        var book = new Book(url) {
            Cover = await GetCover(doc),
            Chapters = await FillChapters(bookId, title),
            Title = title,
            Author = new Author("Витамина Мятная"),
            Annotation = doc.QuerySelector("div.subscribe div.text-center p")?.InnerHtml.HtmlDecode(),
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(string bookId, string title) {
        if (Config.Options.NoChapters) {
            return [];
        }
        
        var chapter = new Chapter();
        var sb = new StringBuilder();
        var pages = await GetPages(bookId);

        for (var i = 1; i <= pages; i++) {
            Config.Logger.LogInformation($"Получаю страницу {i}/{pages}");

            var uri = SystemUrl.MakeRelativeUri($"/read/{bookId}/?page={i}");
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(uri);

            if (doc.DocumentNode.InnerText.Contains("Чтобы продолжить чтение, пожалуйста, оплатите доступ.")) {
                Config.Logger.LogInformation("Платный контент");
                break;
            }

            foreach (var p in doc.QuerySelectorAll("div.book p")) {
                var content = p.GetText();
                if (!string.IsNullOrWhiteSpace(content)) {
                    sb.AppendLine(content.CoverTag("p"));
                }
            }
        }

        chapter.Title = title;
        chapter.Content = sb.AsHtmlDoc().DocumentNode.InnerHtml;

        return new[] { chapter };
    }

    private async Task<int> GetPages(string bookId) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri($"/read/{bookId}/"));
        var pages = doc.QuerySelectorAll("div.navigation > a[href]")
            .Where(a => int.TryParse(a.GetText(), out _))
            .Select(a => int.Parse(a.GetText()))
            .ToList();
        
        return pages.Any() ? pages.Max() : 1;
    }

    private Task<TempFile> GetCover(HtmlDocument doc) {
        var imagePath = doc.QuerySelector("div.subscribe img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(SystemUrl.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
}