using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Neobook;
using HtmlAgilityPack;

namespace Elib2Ebook.Logic.Getters;

public class NeobookGetter : GetterBase {
    public NeobookGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("http://neobook.org/");

    private Uri _apiUrl = new("https://nbapi.net/");

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

        Console.WriteLine("Успешно авторизовались");
        Config.CookieContainer.Add(SystemUrl, new Cookie("utoken", data?.Login.Utoken));
        Config.CookieContainer.Add(SystemUrl, new Cookie("uid", data?.Login.Uid));
    }

    private MultipartFormDataContent GenerateAuthData() {
        return new() {
            { new StringContent("2.5"), "version" },
            { new StringContent("0"), "uid" },
            { new StringContent(string.Empty), "token" },
            { new StringContent("authorization"), "resource" },
            { new StringContent("login_by_email"), "action" },
            { new StringContent(Config.Options.Login), "email" },
            { new StringContent(Config.Options.Password), "password" },
            { new StringContent("3"), "device_type" },
        };
    }

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = SystemUrl.MakeRelativeUri("/book/" + bookId);
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

        foreach (var neobookChapter in SliceToc(data.Chapters)) {
            Console.WriteLine($"Загружаю главу {neobookChapter.Title.CoverQuotes()}");
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

    private async Task<HtmlDocument> GetChapter(NeobookChapter neobookChapter, string bookId) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri("reader").AppendQueryParameter("book", bookId).AppendQueryParameter("chapter", neobookChapter.Token));
        var base64 = Regex.Match(doc.ParsedText, "var chapter_delta_base64 = \'(?<data>.*?)\'").Groups["data"].Value;
        if (string.IsNullOrWhiteSpace(base64)) {
            return default;
        }

        var neobookOps = Encoding.UTF8.GetString(Convert.FromBase64String(base64)).Deserialize<NeobookOps>();

        var sb = new StringBuilder();
        foreach (var op in neobookOps.Ops) {
            if (op.Insert != "\n") {
                sb.Append(op.Insert.HtmlDecode().CoverTag("p"));
            }
        }

        return sb.AsHtmlDoc();
    }

    private Task<Image> GetCover(NeobookPostData data) {
        var imagePath = data.Attachment?.Cover?.GetValueOrDefault("l");
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(imagePath.AsUri()) : Task.FromResult(default(Image));
    }
}