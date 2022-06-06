using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Elib2Ebook.Configs;
using Elib2Ebook.Types.Book;
using HtmlAgilityPack;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Litnet;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters;

public abstract class LitnetGetterBase : GetterBase {
    public LitnetGetterBase(BookGetterConfig config) : base(config) { }
    
    private static readonly string DeviceId = Guid.NewGuid().ToString().ToUpper();
    private const string SECRET = "14a6579a984b3c6abecda6c2dfa83a64";

    private string _token { get; set; }

    protected override string GetId(Uri url) {
        return base.GetId(url).Split('-').Last().Replace("b", string.Empty);
    }

    private static string Decrypt(string text) {
        using var aes = Aes.Create();
        const int IV_SHIFT = 16;
        
        aes.Key = Encoding.UTF8.GetBytes(SECRET); 
        aes.IV = Encoding.UTF8.GetBytes(text)[..IV_SHIFT];
        
        var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(Convert.FromBase64String(text));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        
        var output = new MemoryStream();
        cs.CopyTo(output);

        return Encoding.UTF8.GetString(output.ToArray()[IV_SHIFT..]);
    }

    private static string GetSign(string token) {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.ASCII.GetBytes(DeviceId + SECRET + (token ?? string.Empty));
        var hashBytes = md5.ComputeHash(inputBytes);

        return Convert.ToHexString(hashBytes).ToLower();
    }
    
    /// <summary>
    /// Авторизация в системе
    /// </summary>
    /// <exception cref="Exception"></exception>
    public override async Task Authorize() {
        var path = _config.HasCredentials ? "user/find-by-login" : "registration/registration-by-device";

        var url = $"https://api.{SystemUrl.Host}/v1/{path}?login={_config.Login?.TrimStart('+') ?? string.Empty}&password={HttpUtility.UrlEncode(_config.Password)}&app=android&device_id={DeviceId}&sign={GetSign(string.Empty)}";
        var response = await _config.Client.GetFromJsonAsync<LitnetAuthResponse>(url);

        if (!string.IsNullOrWhiteSpace(response?.Token)) {
            Console.WriteLine("Успешно авторизовались");
            _token = response.Token;
        } else {
            throw new Exception($"Не удалось авторизоваться. {response?.Error}");
        }
    }

    private async Task<LitnetBookResponse> GetBook(string token, string bookId) {
        var url = $"https://api.{SystemUrl.Host}/v1/book/get/{bookId}?app=android&device_id={DeviceId}&user_token={token}&sign={GetSign(token)}";
        var response = await _config.Client.GetFromJsonAsync<LitnetBookResponse>(url);
        return response;
    }

    private async Task<LitnetContentsResponse[]> GetBookContents(string token, string bookId) {
        var url = $"https://api.{SystemUrl.Host}/v1/book/contents?bookId={bookId}&app=android&device_id={DeviceId}&user_token={token}&sign={GetSign(token)}";
        var response = await _config.Client.GetFromJsonAsync<LitnetContentsResponse[]>(url);
        return response;
    }

    private async Task<LitnetChapterResponse[]> GetChapters(string token, LitnetContentsResponse[] contents) {
        var chapters = string.Join("&", contents.Select(t => $"chapter_ids[]={t.Id}"));
        var url = $"https://api.{SystemUrl.Host}/v1/book/get-chapters-texts/?{chapters}&app=android&device_id={DeviceId}&sign={GetSign(token)}&user_token={token}";
        var response = await _config.Client.GetFromJsonAsync<LitnetChapterResponse[]>(url);
        return response;
    }
    
    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);

        var litnetBook = await GetBook(_token, bookId);

        var uri = new Uri(litnetBook.Url);
        var book = new Book(uri) {
            Cover = await GetCover(litnetBook),
            Chapters = await FillChapters(_token, litnetBook, bookId),
            Title = litnetBook.Title.Trim(),
            Author = GetAuthor(litnetBook),
            Annotation = GetAnnotation(litnetBook),
            Seria = await GetSeria(uri)
        };
            
        return book;
    }

    private async Task<Seria> GetSeria(Uri url) {
        try {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
            var a = doc.QuerySelector("div.book-view-info-coll a[href*='sort=cycles']");
            if (a != default) {
                return new Seria {
                    Name = a.GetText(),
                    Url = new Uri(url, a.Attributes["href"].Value)
                };
            }
        } catch (Exception ex) {
            Console.WriteLine(ex);
        }

        return default;
    }

    private Author GetAuthor(LitnetBookResponse book) {
        return new Author((book.AuthorName ?? SystemUrl.Host).Trim(), new Uri($"https://{SystemUrl.Host}/ru/{book.AuthorId}"));
    }

    private static string GetAnnotation(LitnetBookResponse book) {
        return string.IsNullOrWhiteSpace(book.Annotation) ? 
            string.Empty : 
            string.Join("", book.Annotation.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(s => $"<p>{s.Trim()}</p>"));
    }
    
    private Task<Image> GetCover(LitnetBookResponse book) {
        return !string.IsNullOrWhiteSpace(book.Cover) ? GetImage(new Uri(book.Cover)) : Task.FromResult(default(Image));
    }

    private async Task<List<Chapter>> FillChapters(string token, LitnetBookResponse book, string bookId) {
        var result = new List<Chapter>();
            
        var contents = await GetBookContents(token, bookId);
        var chapters = await GetChapters(token, contents);

        var map = chapters.ToDictionary(t => t.Id);
        
        foreach (var content in contents) {
            var litnetChapter = map[content.Id];

            Console.WriteLine($"Загружаю главу {content.Title.Trim().CoverQuotes()}");
            if (string.IsNullOrWhiteSpace(litnetChapter.Text)) {
                Console.WriteLine($"Главу {content.Title.Trim().CoverQuotes()} в платном доступе");
                continue;
            }
            
            var chapter = new Chapter();

            var chapterDoc = GetChapter(litnetChapter);
            chapter.Images = await GetImages(chapterDoc, new Uri("https://litnet.com"));
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = (content.Title ?? book.Title).Trim();

            result.Add(chapter);
        }

        return result;
    }

    private static HtmlDocument GetChapter(LitnetChapterResponse chapter) {
        return Decrypt(chapter.Text).Deserialize<string[]>().Aggregate(new StringBuilder(), (sb, row) => sb.Append(row)).AsHtmlDoc();
    }
}