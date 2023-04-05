using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.DarkNovels;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters;

public class DarkNovelsGetter : GetterBase {
    private static readonly Dictionary<int, char> _map = new();
    private const string ALPHABET = "аАбБвВгГдДеЕёЁжЖзЗиИйЙкКлЛмМнНоОпПрРсСтТуУфФхХцЦчЧшШщЩъЪыЫьЬэЭюЮяЯ";

    public DarkNovelsGetter(BookGetterConfig config) : base(config) {
        InitMap();
    }

    protected override Uri SystemUrl => new("https://dark-novels.ru/");

    private Uri _apiUrl => new($"https://api.{SystemUrl.Host}/");

    public override Task Init() {
        Config.Client.DefaultRequestVersion = HttpVersion.Version20;
        Config.Client.DefaultRequestHeaders.Add("User-Agent", "novels 1.0.3");
        return Task.CompletedTask;
    }

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }

        var payload = new {
            email = Config.Options.Login,
            password = Config.Options.Password
        };

        var response = await Config.Client.PostAsJsonAsync(_apiUrl.MakeRelativeUri("/v1/users/login"), payload);
        var data = await response.Content.ReadFromJsonAsync<DarkNovelsData<DarkNovelsAuthResponse>>();
        if (data!.Status == "success") {
            Config.Client.DefaultRequestHeaders.Add("Token", data.Data.Token.AccessToken);
            Console.WriteLine("Успешно авторизовались");
        } else {
            throw new Exception($"Не удалось авторизоваться. {data.Message}");
        }
    }

    private static void InitMap() {
        var start = 13338;
        const int shift = 38;
        foreach (var c in ALPHABET) {
            for (var i = start; i < start + shift; i++) {
                _map[i] = c;
            }

            start += shift;
        }
    }

    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);

        var bookFullId = GetId(url);
        var bookId = bookFullId.Split(".").Last();

        var uri = SystemUrl.MakeRelativeUri($"/{bookFullId}/");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(uri);

        var book = new Book(uri) {
            Cover = await GetCover(doc, uri),
            Chapters = await FillChapters(bookId),
            Title = doc.GetTextBySelector("h2.display-1"),
            Author = GetAuthor(doc),
            Annotation = doc.QuerySelector("div.description")?.InnerHtml
        };

        return book;
    }

    private static Author GetAuthor(HtmlDocument doc) {
        var match = Regex.Match(doc.ParsedText, "authors:\"(?<author>.*?)\"");
        return new Author(match.Success ? match.Groups["author"].Value : "DarkNovels");
    }

    private async Task<Uri> GetMainUrl(Uri url) {
        if (url.GetSegment(1) == "read") {
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
            var id = Regex.Match(doc.DocumentNode.InnerHtml, "slug:\"(?<id>.*?)\"");
            return SystemUrl.MakeRelativeUri($"/{id.Groups["id"].Value}");
        }

        return url;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(string bookId) {
        var result = new List<Chapter>();

        foreach (var darkNovelsChapter in await GetToc(bookId)) {
            Console.WriteLine($"Загружаю главу {darkNovelsChapter.Title.CoverQuotes()}");

            var chapter = new Chapter {
                Title = darkNovelsChapter.Title
            };

            await FillChapter(bookId, darkNovelsChapter, chapter);

            result.Add(chapter);
        }

        return result;
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.book-cover-container img")?.Attributes["data-src"]?.Value;
        if (string.IsNullOrWhiteSpace(imagePath)) {
            var match = new Regex("\"image\": \"(?<url>.*?)\"").Match(doc.Text);
            if (match.Success) {
                imagePath = match.Groups["url"].Value;
            }
        }

        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }

    private async Task<IEnumerable<DarkNovelsChapter>> GetToc(string bookId) {
        return await Config.Client.GetFromJsonAsync<DarkNovelsData<DarkNovelsChapter[]>>(_apiUrl.MakeRelativeUri($"/v2/toc/{bookId}"))
            .ContinueWith(t => SliceToc(t.Result?.Data.Where(c => !c.Title.StartsWith("Volume:")).ToList()));
    }

    private async Task FillChapter(string bookId, DarkNovelsChapter darkNovelsChapter, Chapter chapter) {
        var data = await Config.Client.PostWithTriesAsync(_apiUrl.MakeRelativeUri("/v2/chapter/"), GetData(bookId, darkNovelsChapter.Id, "html"), TimeSpan.FromMilliseconds(200));
        if (data.StatusCode == HttpStatusCode.BadRequest) {
            return;
        }
        
        using var zip = new ZipArchive(await data.Content.ReadAsStreamAsync());
        var sb = new StringBuilder();

        foreach (var entry in zip.Entries) {
            using var sr = new StreamReader(entry.Open());
            foreach (var c in await sr.ReadToEndAsync()) {
                sb.Append(_map.GetValueOrDefault(c, c));
            }
        }
        
        var chapterDoc = sb.AsHtmlDoc().RemoveNodes("h1");
        chapter.Images = await GetImages(chapterDoc, SystemUrl);
        chapter.Content = chapterDoc.DocumentNode.InnerHtml;
    }

    private static MultipartFormDataContent GetData(string bookId, int chapterId, string format) {
        return new MultipartFormDataContent {
            { new StringContent(bookId), "b" },
            { new StringContent(format), "f" },
            { new StringContent("l"), "t" },
            { new StringContent(chapterId.ToString()), "c" }
        };
    }
}