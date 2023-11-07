using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Litnet;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters.Litnet;

public abstract class LitnetGetterBase : GetterBase {
    public LitnetGetterBase(BookGetterConfig config) : base(config) { }

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
        var path = Config.HasCredentials ? "user/find-by-login" : "registration/registration-by-device";

        var url = ApiIp.MakeRelativeUri($"v1/{path}?login={HttpUtility.UrlEncode(Config.Options.Login?.TrimStart('+') ?? string.Empty)}&password={HttpUtility.UrlEncode(Config.Options.Password)}&app=android&device_id={DeviceId}&sign={GetSign(string.Empty)}");
        var response = await Config.Client.SendAsync(GetDefaultMessage(url, ApiUrl));
        var data = await response.Content.ReadFromJsonAsync<LitnetAuthResponse>();

        if (!string.IsNullOrWhiteSpace(data?.Token)) {
            Console.WriteLine("Успешно авторизовались");
            _token = data.Token;
        } else {
            throw new Exception($"Не удалось авторизоваться. {data?.Error}");
        }
    }

    private async Task<LitnetBookResponse> GetBook(string token, string bookId) {
        var url = ApiIp.MakeRelativeUri($"/v1/book/get/{bookId}?app=android&device_id={DeviceId}&user_token={token}&sign={GetSign(token)}");
        var response = await Config.Client.SendAsync(GetDefaultMessage(url, ApiUrl));
        var data = await response.Content.ReadFromJsonAsync<LitnetBookResponse>();
        if (!Config.HasCredentials && data!.AdultOnly) {
            throw new Exception("Произведение 18+. Необходимо добавить логин и пароль.");
        }
        
        return data;
    }

    private async Task<LitnetContentsResponse[]> GetBookContents(string token, string bookId) {
        var url = ApiIp.MakeRelativeUri($"/v1/book/contents?bookId={bookId}&app=android&device_id={DeviceId}&user_token={token}&sign={GetSign(token)}");
        var response = await Config.Client.SendAsync(GetDefaultMessage(url, ApiUrl));
        return response.StatusCode == HttpStatusCode.NotFound ? 
            Array.Empty<LitnetContentsResponse>() : 
            await response.Content.ReadFromJsonAsync<LitnetContentsResponse[]>();
    }

    private async Task<IEnumerable<LitnetChapterResponse>> GetToc(string token, IEnumerable<LitnetContentsResponse> contents) {
        var chapters = string.Join("&", contents.Select(t => $"chapter_ids[]={t.Id}"));
        var url = ApiIp.MakeRelativeUri($"/v1/book/get-chapters-texts/?{chapters}&app=android&device_id={DeviceId}&sign={GetSign(token)}&user_token={token}");
        var response = await Config.Client.SendAsync(GetDefaultMessage(url, ApiUrl));
        var data = await response.Content.ReadFromJsonAsync<LitnetChapterResponse[]>();
        return SliceToc(data);
    }
    
    private async Task<HtmlDocument> GetChapterExploit(string token, LitnetChapterResponse chapter) {
        try {
            var url = ApiIp.MakeRelativeUri($"/v1/text/get-chapter?chapter_id={chapter.Id}&app=android&device_id={DeviceId}&sign={GetSign(token)}&user_token={token}");
            var bytes = await Config.Client.GetByteArrayAsync(url);
            var gz = Decrypt(Convert.ToBase64String(bytes));
            return GetChapterDoc(await Unzip(gz));
        } catch {
            return default;
        }
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
        if (contents.Length == 0) {
            return result;
        }
        
        var chapters = await GetToc(token, contents);
        var map = chapters.ToDictionary(t => t.Id);
        
        foreach (var content in contents) {
            var litnetChapter = map[content.Id];

            Console.WriteLine($"Загружаю главу {content.Title.Trim().CoverQuotes()}");
            var chapter = new Chapter {
                Title = (content.Title ?? book.Title).Trim()
            };

            var chapterDoc = string.IsNullOrWhiteSpace(litnetChapter.Text) ? 
                await GetChapterExploit(token, litnetChapter) : 
                GetChapterDoc(Encoding.UTF8.GetString(Decrypt(litnetChapter.Text)));

            if (chapterDoc != default) {
                chapter.Images = await GetImages(chapterDoc, SystemUrl);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }

            result.Add(chapter);
        }

        return result;
    }
    
    private static async Task<string> Unzip(byte[] gz) {
        await using var cs = new MemoryStream(gz);
        await using var zs = new GZipStream(cs, CompressionMode.Decompress);
        await using var rs = new MemoryStream();
        await zs.CopyToAsync(rs);
        return Encoding.UTF8.GetString(rs.ToArray());
    }

    private static HtmlDocument GetChapterDoc(string text) {
        return text.Deserialize<string[]>().Aggregate(new StringBuilder(), (sb, row) => sb.Append(row)).AsHtmlDoc();
    }
}