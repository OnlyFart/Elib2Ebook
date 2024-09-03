using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Litnet;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Core.Logic.Getters.Litnet;

public abstract class LitnetGetterBase : GetterBase {
    protected LitnetGetterBase(BookGetterConfig config) : base(config) { }

    protected Uri ApiUrl => new($"https://api.{SystemUrl.Host}/");
    
    //cloudflare :(
    protected virtual Uri ApiIp => ApiUrl;

    protected virtual Uri SiteIp => SystemUrl;

    private static readonly string DeviceId = Guid.NewGuid().ToString().ToUpper();
    private const string SECRET = "14a6579a984b3c6abecda6c2dfa83a64";

    private string _token { get; set; }

    protected override string GetId(Uri url) => base.GetId(url).Split('-').Last().Replace("b", string.Empty);

    private static byte[] Decrypt(string text) {
        using var aes = Aes.Create();
        const int IV_SHIFT = 16;
        
        aes.Key = Encoding.UTF8.GetBytes(SECRET); 
        aes.IV = Encoding.UTF8.GetBytes(text)[..IV_SHIFT];
        
        var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(Convert.FromBase64String(text));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        
        var output = new MemoryStream();
        cs.CopyTo(output);

        return output.ToArray()[IV_SHIFT..];
    }

    private static string GetSign(string token) {
        var inputBytes = Encoding.ASCII.GetBytes(DeviceId + SECRET + (token ?? string.Empty));
        var hashBytes = MD5.HashData(inputBytes);

        return Convert.ToHexString(hashBytes).ToLower();
    }

    /// <summary>
    /// Авторизация в системе
    /// </summary>
    /// <exception cref="Exception"></exception>
    public override async Task Authorize() {
        var path = Config.HasCredentials ? "user/find-by-login" : "registration/registration-by-device";

        var url = ApiIp.MakeRelativeUri($"v1/{path}?login={HttpUtility.UrlEncode(Config.Options.Login?.TrimStart('+') ?? string.Empty)}&password={HttpUtility.UrlEncode(Config.Options.Password)}&app=android&device_id={DeviceId}&sign={GetSign(string.Empty)}");
        var data = await GetApiData<LitnetAuthResponse>(url);

        if (!string.IsNullOrWhiteSpace(data?.Token)) {
            Console.WriteLine("Успешно авторизовались");
            _token = data.Token;
        } else {
            throw new Exception($"Не удалось авторизоваться. {data?.Error}");
        }
    }

    private async Task<LitnetBookResponse> GetBook(string token, string bookId) {
        var url = ApiIp.MakeRelativeUri($"/v1/book/get/{bookId}?app=android&device_id={DeviceId}&user_token={token}&sign={GetSign(token)}");
        var data = await GetApiData<LitnetBookResponse>(url);
        if (!Config.HasCredentials && data!.AdultOnly) {
            throw new Exception("Произведение 18+. Необходимо добавить логин и пароль.");
        }
        
        return data;
    }

    private Task<LitnetContentsResponse[]> GetBookContents(string token, string bookId) {
        var url = ApiIp.MakeRelativeUri($"/v1/book/contents?bookId={bookId}&app=android&device_id={DeviceId}&user_token={token}&sign={GetSign(token)}");
        return GetApiData<LitnetContentsResponse[]>(url);
    }

    private async Task<IEnumerable<LitnetChapterResponse>> GetToc(string token, IEnumerable<LitnetContentsResponse> contents) {
        var chapters = string.Join("&", contents.Select(t => $"chapter_ids[]={t.Id}"));
        var url = ApiIp.MakeRelativeUri($"/v1/book/get-chapters-texts/?{chapters}&app=android&device_id={DeviceId}&sign={GetSign(token)}&user_token={token}");
        var data = await GetApiData<LitnetChapterResponse[]>(url);
        return SliceToc(data);
    }

    private async Task<T> GetApiData<T>(Uri uri) {
        var response = await Config.Client.SendAsync(GetDefaultMessage(uri, ApiUrl));
        if (response.StatusCode == HttpStatusCode.TooManyRequests) {
            throw new Exception("Получен бан. Попробуйте позже.");
        }

        if (response.StatusCode == HttpStatusCode.NotFound) {
            return default;
        }
        
        var data = await response.Content.ReadFromJsonAsync<T>();
        return data;
    }
    
    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);

        var litnetBook = await GetBook(_token, bookId);

        var uri = SystemUrl.MakeRelativeUri(litnetBook.Url.AsUri().AbsolutePath);
        var book = new Book(uri) {
            Cover = await GetCover(litnetBook),
            Chapters = await FillChapters(_token, litnetBook, bookId),
            Title = litnetBook.Title.Trim(),
            Author = GetAuthor(litnetBook),
            CoAuthors = GetCoAuthors(litnetBook),
            Annotation = GetAnnotation(litnetBook),
            Seria = await GetSeria(uri, litnetBook),
            Lang = litnetBook.Lang
        };
            
        return book;
    }

    private async Task<Seria> GetSeria(Uri url, LitnetBookResponse book) {
        try {
            var doc = await Config.Client.SendAsync(GetDefaultMessage(url.ReplaceHost(SiteIp.Host), SystemUrl)).ContinueWith(t => t.Result.Content.ReadAsStream().AsHtmlDoc());
            var a = doc.QuerySelector("div.book-view-info-coll a[href*='sort=cycles']");
            if (a != default) {
                return new Seria {
                    Name = a.GetText(),
                    Url = url.MakeRelativeUri(a.Attributes["href"].Value),
                    Number = book.CyclePriority is > 0 ? book.CyclePriority.Value.ToString() : string.Empty
                };
            }
        } catch (Exception ex) {
            Console.WriteLine(ex);
        }

        return default;
    }

    private Author GetAuthor(LitnetBookResponse book) {
        return new Author((book.AuthorName ?? SystemUrl.Host).Trim(), SystemUrl.MakeRelativeUri($"/ru/{book.AuthorId}"));
    }
    
    private IEnumerable<Author> GetCoAuthors(LitnetBookResponse book) {
        var result = new List<Author>();
        if (!string.IsNullOrWhiteSpace(book.CoAuthorName) && book.CoAuthorId.HasValue) {
            result.Add(new Author((book.CoAuthorName ?? SystemUrl.Host).Trim(), SystemUrl.MakeRelativeUri($"/ru/{book.CoAuthorId.Value}")));
        }

        return result;
    }

    private static string GetAnnotation(LitnetBookResponse book) {
        return string.IsNullOrWhiteSpace(book.Annotation) ? 
            string.Empty : 
            string.Join("", book.Annotation.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().CoverTag("p")));
    }
    
    private Task<Image> GetCover(LitnetBookResponse book) {
        return !string.IsNullOrWhiteSpace(book.Cover) ? SaveImage(book.Cover.AsUri()) : Task.FromResult(default(Image));
    }
    
    protected override HttpRequestMessage GetImageRequestMessage(Uri uri) {
        if (uri.IsSameHost(SystemUrl) || uri.IsSameSubDomain(SystemUrl)) {
            return GetDefaultMessage(SystemUrl.MakeRelativeUri(uri.AbsolutePath), uri);
        }

        return base.GetImageRequestMessage(uri);
    }
    
    private HttpRequestMessage GetDefaultMessage(Uri uri, Uri host, HttpContent content = null) {
        var message = new HttpRequestMessage(content == default ? HttpMethod.Get : HttpMethod.Post, uri);
        message.Content = content;
        message.Version = Config.Client.DefaultRequestVersion;
        
        foreach (var header in Config.Client.DefaultRequestHeaders) {
            message.Headers.Add(header.Key, header.Value);
        }

        message.Headers.Host = host.Host;

        return message;
    }

    private async Task<List<Chapter>> FillChapters(string token, LitnetBookResponse book, string bookId) {
        var result = new List<Chapter>();
            
        var contents = await GetBookContents(token, bookId);
        if (contents == default || contents.Length == 0) {
            return result;
        }
        
        var chapters = await GetToc(token, contents);
        var map = chapters.ToDictionary(t => t.Id);
        
        foreach (var content in contents) {
            if (!map.TryGetValue(content.Id, out var litnetChapter)) {
                litnetChapter = new LitnetChapterResponse();
            }

            Console.WriteLine($"Загружаю главу {content.Title.Trim().CoverQuotes()}");
            var chapter = new Chapter {
                Title = (content.Title ?? book.Title).Trim()
            };
            
            if (!string.IsNullOrWhiteSpace(litnetChapter.Text)) {
                var chapterDoc = GetChapterDoc(Encoding.UTF8.GetString(Decrypt(litnetChapter.Text)));
                chapter.Images = await GetImages(chapterDoc, SystemUrl);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }

            result.Add(chapter);
        }

        return result;
    }

    private static HtmlDocument GetChapterDoc(string text) {
        return text.Deserialize<string[]>().Aggregate(new StringBuilder(), (sb, row) => sb.Append(row)).AsHtmlDoc();
    }
}