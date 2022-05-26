using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Litres;
using Elib2Ebook.Types.Litres.Requests;
using Elib2Ebook.Types.Litres.Response;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class LitresGetter : GetterBase {
    private const string SECRET_KEY = "AsAAfdV000-1kksn6591x:[}A{}<><DO#Brn`BnB6E`^s\"ivP:RY'4|v\"h/r^]";
    private const string APP = "4";
    
    public LitresGetter(BookGetterConfig config) : base(config) { }
    
    protected override Uri SystemUrl => new("https://www.litres.ru");
    
    private static Uri GetShortUri(string bookId) => new($"https://catalit.litres.ru/pub/t/{bookId}.fb3");

    private static long GetCurrentMilli()  {
        var jan1970 = new DateTime(1970, 1, 1, 0, 0, 0,DateTimeKind.Utc);
        var javaSpan = DateTime.UtcNow - jan1970;
        return ((long)javaSpan.TotalMilliseconds) / 1000;
    }
    
    private Uri GetFullUri(string bookId) {
        var ts = GetCurrentMilli();
        using var md5 = MD5.Create();

        var inputBytes = Encoding.ASCII.GetBytes($"{ts}:{bookId}:{SECRET_KEY}");
        var hashBytes = md5.ComputeHash(inputBytes);

        return new($"https://catalit.litres.ru/pages/catalit_download_book/?type=fb3&art={bookId}&sid={_authData.Sid}&uilang=ru&libapp={APP}&timestamp={ts}&md5={Convert.ToHexString(hashBytes).ToLower()}");
    }

    private LitresAuthResponseData _authData;

    public override async Task Authorize() {
        if (!_config.HasCredentials) {
            return;
        }
        
        var payload = LitresPayload.Create(DateTime.Now, string.Empty, SECRET_KEY, APP);
        payload.Requests.Add(new LitresAuthRequest(_config.Login, _config.Password));

        _authData = await GetResponse<LitresAuthResponseData>(payload);
        
        if (!_authData.Success) {
            throw new Exception($"Не удалось авторизоваться. {_authData.ErrorMessage}");
        }
    }

    private async Task<T> GetResponse<T>(LitresPayload payload) {
        var resp = await _config.Client.PostWithTriesAsync(new Uri("https://catalit.litres.ru/catalitv2"), CreatePayload(payload));
        return await resp.Content.ReadAsStringAsync().ContinueWith(t => t.Result.Deserialize<LitresResponse<T>>().Data);
    }

    private static FormUrlEncodedContent CreatePayload(LitresPayload payload) {
        var d = new Dictionary<string, string> {
            ["jdata"] = JsonSerializer.Serialize(payload)
        };

        return new FormUrlEncodedContent(d);
    }

    private static Uri GetMainUrl(Uri url) {
        var art = url.GetQueryParameter("art");
        return string.IsNullOrWhiteSpace(art) ? url : new Uri($"https://www.litres.ru/{art}");
    }

    public override async Task<Book> Get(Uri url) {
        url = GetMainUrl(url);
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
        var bookId = doc.QuerySelector("input[name=art]").Attributes["value"].Value;

        var title = doc.GetTextBySelector("h1");
        
        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(bookId, title),
            Title = title,
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div.biblio_book_descr_publishers")?.InnerHtml,
        };
        
        return book;
    }
    
    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var author = doc.QuerySelector("a.biblio_book_author__link");
        return new Author(author.GetTextBySelector(), new Uri(url, author.Attributes["href"]?.Value ?? string.Empty));
    }
    
    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = (doc.QuerySelector("meta[property=og:image]")?.Attributes["content"] ?? doc.QuerySelector("img[itemprop=image]")?.Attributes["data-src"])?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(string bookId, string title) {
        var result = new List<Chapter>();
        var book = await GetBook(bookId);
        
        foreach (var section in book.Content.QuerySelectorAll("section")) {
            if (section.QuerySelector("> section") != null) {
                continue;
            }
            
            var chapter = new Chapter {
                Title = (section.GetTextBySelector("title") ?? title).ReplaceNewLine()
            };
            
            section.RemoveNodes("title, note, clipped");
            chapter.Images = GetImages(section, book);
            chapter.Content = section.InnerHtml;
            result.Add(chapter);
        }

        return result;
    }

    private static IEnumerable<Image> GetImages(HtmlNode doc, LitresBook book) {
        var images = new List<Image>();
        foreach (var img in doc.QuerySelectorAll("img")) {
            var path = img.Attributes["src"]?.Value;
            if (string.IsNullOrWhiteSpace(path)) {
                img.Remove();
                continue;
            }
        
            if (!book.Targets.TryGetValue(path, out var t)) {
                img.Remove();
                continue;
            }
            
            if (t?.Content == null || t.Content.Length == 0) {
                img.Remove();
                continue;
            }

            var fileName = t.Target.Split("/").Last().Trim('/');
            img.Attributes["src"].Value = fileName;
            images.Add(new Image(fileName, t.Content));
        }

        return images;
    }

    private async Task<LitresBook> GetBook(string bookId) {
        var uri = _authData == null ? GetShortUri(bookId) : GetFullUri(bookId);
        var response = await _config.Client.GetAsync(uri);
        if (response.StatusCode != HttpStatusCode.OK || !response.Headers.AcceptRanges.Any()) {
            response = await _config.Client.GetAsync(GetShortUri(bookId));
        }

        var result = new LitresBook();

        if (response.StatusCode != HttpStatusCode.OK) {
            return result;
        }
        
        var map = new Dictionary<string, byte[]>();
        using var zip = new ZipArchive(await response.Content.ReadAsStreamAsync());

        foreach (var entry in zip.Entries) {
            using var ms = new MemoryStream();
            await entry.Open().CopyToAsync(ms);
            map[entry.FullName.Replace("fb3/", string.Empty)] = ms.ToArray();
        }

        foreach (var (key, value) in map) {
            if (key.EndsWith("body.xml")) {
                result.Content = Encoding.UTF8.GetString(value).AsXHtmlDoc();
            } else if (key.EndsWith("body.xml.rels")) {
                foreach (var r in Encoding.UTF8.GetString(value).AsXHtmlDoc().QuerySelectorAll("Relationship")) {
                    var id = r.Attributes["Id"]?.Value;
                    var target = r.Attributes["Target"]?.Value;
                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(target)) {
                        continue;
                    }

                    if (!map.TryGetValue(target, out var t)) {
                        continue;
                    }
                    
                    result.Targets[id] = new LitresTarget {
                        Id = id,
                        Target = target,
                        Content = t
                    };
                }
            }
        }

        return result;
    }
}