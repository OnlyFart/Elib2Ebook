using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Neobook;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters;

public class NeobookGetter : GetterBase {
    public NeobookGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("http://neobook.org/");

    private Uri _apiUrl = new("https://api.neobook.org/");

    protected override string GetId(Uri url) {
        return url.GetSegment(1) == "book" ? url.GetSegment(2) : url.GetQueryParameter("book");
    }

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }

        var response = await Config.Client.PostWithTriesAsync(_apiUrl, GenerateAuthData());
        var data = await response.Content.ReadFromJsonAsync<NeobookAuth>();
        if (!string.IsNullOrWhiteSpace(data?.Error)) {
            throw new Exception($"Не удалось авторизоваться. {data.Error}");
        }

        Config.Logger.LogInformation("Успешно авторизовались");
        Config.CookieContainer.Add(SystemUrl, new Cookie("utoken", data?.Login.Utoken));
        Config.CookieContainer.Add(SystemUrl, new Cookie("uid", data?.Login.Uid));
    }

    private MultipartFormDataContent GenerateAuthData() {
        return new() {
            { new StringContent("3.1"), "version" },
            { new StringContent("0"), "uid" },
            { new StringContent(string.Empty), "utoken" },
            { new StringContent("authorization"), "resource" },
            { new StringContent("login_by_email"), "action" },
            { new StringContent(Config.Options.Login), "email" },
            { new StringContent(Config.Options.Password), "password" },
            { new StringContent("3"), "platform_id" },
        };
    }

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = SystemUrl.MakeRelativeUri("/book/" + bookId + "/");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        var data = GetPostData(doc);

        var book = new Book(url) {
            Cover = await GetCover(data),
            Chapters = await FillChapters(data, bookId),
            Title = data.Title,
            Author = GetAuthor(data),
            Annotation = data.Text
        };

        return book;
    }

    private static NeobookPostData GetPostData(HtmlDocument doc) {
        var data = Regex.Match(doc.ParsedText, "var postData = (?<data>.*?);\n").Groups["data"].Value;
        return data.Deserialize<NeobookPostData>();
    }

    private Author GetAuthor(NeobookPostData data) {
        return new Author(data.User.LastName + " " + data.User.FirstName, SystemUrl.MakeRelativeUri(data.User.UserName));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(NeobookPostData data, string bookId) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        foreach (var neobookChapter in SliceToc(data.Chapters)) {
            Config.Logger.LogInformation($"Загружаю главу {neobookChapter.Title.CoverQuotes()}");
            var chapter = new Chapter {
                Title = neobookChapter.Title
            };
            
            var chapterDoc = await GetChapter(neobookChapter, bookId);
            if (chapterDoc != default) {
                chapter.Images = await GetImages(chapterDoc, SystemUrl);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(NeobookTocChapter neobookTocChapter, string bookId) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri("reader/").AppendQueryParameter("book", bookId).AppendQueryParameter("chapter", neobookTocChapter.Token));
        var data = Regex.Match(doc.ParsedText, "var data = (?<data>.*?);\n").Groups["data"].Value;
        if (string.IsNullOrWhiteSpace(data)) {
            return default;
        }
        
        var neobookBook = data.Deserialize<NeobookBook>();
        if (neobookBook.ActiveChapterIndex > neobookBook.Chapters.Length) {
            return default;
        }

        var chapter = neobookBook.Chapters[neobookBook.ActiveChapterIndex];
        if (string.IsNullOrWhiteSpace(chapter.Data?.Html)) {
            return default;
        }
        
        return chapter.Data.Html.AsHtmlDoc();
    }

    private Task<Image> GetCover(NeobookPostData data) {
        var imagePath = data.Attachment?.Cover?.GetValueOrDefault("l");
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(imagePath.AsUri()) : Task.FromResult(default(Image));
    }
}