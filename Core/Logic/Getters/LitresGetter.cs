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
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using Core.Types.Litres;
using Core.Types.Litres.Requests;
using Core.Types.Litres.Response;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class LitresGetter : GetterBase {
    private const string SECRET_KEY = "AsAAfdV000-1kksn6591x:[}A{}<><DO#Brn`BnB6E`^s\"ivP:RY'4|v\"h/r^]";
    private const string APP = "13";
    
    public LitresGetter(BookGetterConfig config) : base(config) { }

    protected override Uri SystemUrl => new("https://www.litres.ru");
    
    private static Uri GetShortUri(string bookId) => new($"https://catalit.litres.ru/pub/t/{bookId}.fb3");

    private static long GetCurrentMilli()  {
        var jan1970 = new DateTime(1970, 1, 1, 0, 0, 0,DateTimeKind.Utc);
        var javaSpan = DateTime.UtcNow - jan1970;
        return (long)javaSpan.TotalMilliseconds / 1000;
    }

    private Uri GetFullUri(string bookId, string path, LitresFile file) {
        var ts = GetCurrentMilli();

        var inputBytes = Encoding.ASCII.GetBytes($"{ts}:{bookId}:{SECRET_KEY}");
        var hashBytes = MD5.HashData(inputBytes);

        if (file == default) {
            var uri = new Uri($"https://catalit.litres.ru/pages/download_book_j?art={bookId}&sid={_authData.Sid}&uilang=ru&libapp={APP}&timestamp={ts}&md5={Convert.ToHexString(hashBytes).ToLower()}");
            return uri.AppendQueryParameter("type", "fb3");
        }

        if (file.Mime.StartsWith("audio")) {
            var uri = new Uri($"https://mvideo.litres.ru/pages/download_book_subscr/{bookId}/{file.Id}.mp3?sid={_authData.Sid}");
            return uri;
        }

        var result = new Uri($"https://catalit.litres.ru/pages/{path}?art={bookId}&sid={_authData.Sid}&uilang=ru&libapp={APP}&timestamp={ts}&md5={Convert.ToHexString(hashBytes).ToLower()}");
        result = result.AppendQueryParameter("file", file.Id);
        if (!string.IsNullOrWhiteSpace(file.Extension)) {
            result = result.AppendQueryParameter("type", file.Extension);
        }


        return result;
    }

    private LitresAuthResponseData _authData = new(){
        Sid = "6ufp4b2wbx1acc4k1f0wdo571762c0fn"
    };

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            Config.Client.DefaultRequestHeaders.Add("Session-Id", _authData.Sid);
            return;
        }
        
        // Литрес очень не любит много авторизовывать
        // поэтому пришлось добавить кеширование токенов
        const string directory = "LitresCache";

        if (!Directory.Exists(directory)) {
            Directory.CreateDirectory(directory);
        }

        var saveCreds = $"{directory}/{Config.Options.Login.RemoveInvalidChars()}";
        if (File.Exists(saveCreds)) {
            _authData = await File.ReadAllTextAsync(saveCreds).ContinueWith(t => t.Result.Deserialize<LitresAuthResponseData>());
            Config.Client.DefaultRequestHeaders.Add("Session-Id", _authData.Sid);
            return;
        }

        var payload = LitresPayload.Create(DateTime.Now, string.Empty, SECRET_KEY, APP);
        payload.Requests.Add(new LitresAuthRequest(Config.Options.Login, Config.Options.Password));
     
        _authData = await GetResponse<LitresAuthResponseData>(payload);

        if (!_authData.Success) {
            throw new Exception($"Не удалось авторизоваться. {_authData.ErrorMessage}");
        }
        
        Config.Client.DefaultRequestHeaders.Add("Session-Id", _authData.Sid);
        await File.WriteAllTextAsync(saveCreds, JsonSerializer.Serialize(_authData));
    }

    private async Task<T> GetResponse<T>(LitresPayload payload) {
        var resp = await Config.Client.PostWithTriesAsync("https://catalit.litres.ru/catalitv2".AsUri(), CreatePayload(payload));
        return await resp.Content.ReadAsStringAsync().ContinueWith(t => t.Result.Deserialize<LitresResponse<T>>().Data);
    }
    
    private async Task<T> GetResponse<T>(Uri url) {
        var resp = await Config.Client.GetWithTriesAsync(url);
        if (resp == default) {
            return default;
        }
        
        return await resp.Content.ReadAsStringAsync().ContinueWith(t => t.Result.Deserialize<LitresStaticResponse<T>>().Payload.Data);
    }

    private static FormUrlEncodedContent CreatePayload(LitresPayload payload) {
        var d = new Dictionary<string, string> {
            ["jdata"] = JsonSerializer.Serialize(payload)
        };

        return new FormUrlEncodedContent(d);
    }

    public override async Task<Book> Get(Uri url) {
        var bookId = GetBookId(url);

        var payload = LitresPayload.Create(DateTime.Now, _authData.Sid, SECRET_KEY, APP);
        payload.Requests.Add(new LitresBrowseArtsRequest([bookId]));

        var art = await GetResponse<LitresArts>(payload).ContinueWith(t => t.Result.Arts[0]);
        
        var book = new Book(SystemUrl.MakeRelativeUri(bookId)) {
            Cover = await GetCover(art.Cover),
            Title = art.Title,
            Author = await GetAuthor(art),
            CoAuthors = await GetCoAuthors(art),
            Annotation = art.Annotation,
            Seria = GetSeria(art)
        };

        await FillAdditional(book, bookId);
        var fb3File = book.AdditionalFiles.GetBooks().FirstOrDefault(f => f.Extension == ".fb3");

        if (fb3File == default) {
            Config.Logger.LogInformation("Нет файла fb3. Сформировать файл книги невозможно");
        } else {
            try {
                var litresBook = await GetBook(fb3File.GetStream());
                book.Chapters = await FillChapters(litresBook, art.Title);
            } catch (Exception) {
                Config.Logger.LogInformation($"Не удалось обработать оригинальный файл {fb3File.FullName}");
            }
        }

        return book;
    }

    private async Task FillAdditional(Book book, string bookId) {
        if (_authData != default) {
            if (Config.Options.Additional) {
                var files = await GetResponse<LitresFiles[]>($"https://api.litres.ru/foundation/api/arts/{bookId}/files/grouped".AsUri());

                foreach (var file in files.SelectMany(f => f.Files)) {
                    using var fileResponse = await GetFileResponse(bookId, file);
                    if (fileResponse != default) {
                        var tempFile = await CreateTempFile(fileResponse);
                        if (file.Mime.StartsWith("audio")) {
                            book.AdditionalFiles.AddAudio(tempFile);
                        } else {
                            book.AdditionalFiles.AddBook(tempFile);
                        }
                    }
                }
            } else {
                using var fileResponse = await GetFileResponse(bookId, null);
                if (fileResponse != default) {
                    var tempFile = await CreateTempFile(fileResponse);
                    book.AdditionalFiles.AddBook(tempFile);

                }
            }
        }

        if (_authData == default || book.AdditionalFiles.GetBooks().Count == 0) {
            var fileResponse = await GetShortBook(bookId);
            if (fileResponse != default) {
                book.AdditionalFiles.AddBook(await CreateTempFile(fileResponse));
            }
        }
    }

    private async Task<TempFile> CreateTempFile(HttpResponseMessage response) {
        return await TempFile.Create(response.RequestMessage.RequestUri, Config.TempFolder.Path, response.Content.Headers.ContentDisposition?.FileName?.Trim('\"') ?? response.RequestMessage.RequestUri.GetFileName(), await response.Content.ReadAsStreamAsync());
    }

    private static string GetBookId(Uri url) {
        var art = url.GetQueryParameter("art");
        if (!string.IsNullOrWhiteSpace(art)) {
            return art;
        }
        
        art = url.GetSegment(url.Segments.Length - 1).Split("-").Last();
        if (long.TryParse(art, out _)) {
            return art;
        }

        throw new Exception("Не удалось определить artId");
    }

    private Seria GetSeria(LitresArt art) {
        return art
            .Sequences
            .Select(s => new Seria {
                Name = s.Name, 
                Url = SystemUrl.MakeRelativeUri("serii-knig/").AppendQueryParameter("id", s.Id), 
                Number = s.SequenceNumber
            }).FirstOrDefault();
    }
    
    private async Task<Author> GetAuthor(LitresArt art) {
        var person = art.Persons.FirstOrDefault(a => a.Type == "0");
        var author = person == null ? default : await GetResponse<LitresPerson<long>>($"https://api.litres.ru/foundation/api/persons/{person.Id}".AsUri());
        return author == default ? 
            new Author("Litres") : 
            new Author(author.FullName, SystemUrl.MakeRelativeUri(author.Url));
    }
    
    private async Task<IEnumerable<Author>> GetCoAuthors(LitresArt art) {
        var result = new List<Author>();
        foreach (var person in art.Persons.Where(a => a.Type == "0").Skip(1)) {
            var author = person == null ? default : await GetResponse<LitresPerson<long>>($"https://api.litres.ru/foundation/api/persons/{person.Id}".AsUri());
            if (author != default) {
                result.Add(new Author(author.FullName, SystemUrl.MakeRelativeUri(author.Url)));
            }
        }

        return result;
    }
    
    private Task<TempFile> GetCover(string imagePath) {
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(imagePath.AsUri()) : Task.FromResult(default(TempFile));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(LitresBook book, string title) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }
        
        foreach (var section in book.Content.QuerySelectorAll("section")) {
            if (section.QuerySelector("> section") != null) {
                continue;
            }
            
            var chapter = new Chapter {
                Title = (section.GetTextBySelector("title") ?? title).ReplaceNewLine()
            };
            
            section.RemoveNodes("title, note, clipped");
            chapter.Images = await GetImages(section, book);
            chapter.Content = section.InnerHtml;
            result.Add(chapter);
        }

        return result;
    }

    private async Task<IEnumerable<TempFile>> GetImages(HtmlNode doc, LitresBook book) {
        var images = new List<TempFile>();
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
            var image = await TempFile.Create(null, Config.TempFolder.Path, fileName, t.Content);
            img.Attributes["src"].Value = image.FullName;
            images.Add(image);
        }

        return images;
    }

    private async Task<HttpResponseMessage> GetFileResponse(string bookId, LitresFile file) {
        if (_authData != null) {
            var uri = GetFullUri(bookId, "download_book_j", file);
            var response = await Config.Client.GetAsync(uri);
            if (response.StatusCode == HttpStatusCode.OK && response.Headers.AcceptRanges.Any()) {
                Config.Logger.LogInformation($"Дополнительный файл доступен по ссылке {uri}");
                return response;
            }
            
            uri = GetFullUri(bookId, "download_book_subscr", file);
            response = await Config.Client.GetAsync(uri);
            if (response.StatusCode == HttpStatusCode.OK && response.Headers.AcceptRanges.Any()) {
                Config.Logger.LogInformation($"Дополнительный файл доступен по ссылке {uri}");
                return response;
            }
        }

        return default;
    }

    private async Task<HttpResponseMessage> GetShortBook(string bookId) {
        var shortUri = GetShortUri(bookId);
        Config.Logger.LogInformation($"Дополнительный файл доступен по ссылке {shortUri}");
        return await Config.Client.GetAsync(shortUri);
    }

    private async Task<LitresBook> GetBook(Stream stream) {
        var result = new LitresBook();
        
        var map = new Dictionary<string, byte[]>();
        using var zip = new ZipArchive(stream);

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