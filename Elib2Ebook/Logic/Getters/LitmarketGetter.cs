using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Elib2Ebook.Configs;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Litmarket;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Elib2Ebook.Extensions;

namespace Elib2Ebook.Logic.Getters; 

public class LitmarketGetter : GetterBase {
    public LitmarketGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://litmarket.ru");
    
    // cloudflare :(
    private const string HOST = "89.108.111.237";

    public override async Task<Book> Get(Uri url) {
        Init();
        
        var bookId = GetId(url);
        url = new Uri($"https://{HOST}/books/{bookId}");
        var doc = await Init(url);
        await Authorize();
        var content = await GetMainData(bookId);
            
        var blocks = await GetBlocks(content.Book.EbookId);

        var title = Normalize(doc.GetTextBySelector("h1.card-title"));
        var book = new Book {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(GetToc(content, title), blocks, url, content.Book.EbookId, title),
            Title = title,
            Author = Normalize(doc.GetTextBySelector("div.card-author").Replace("Автор:", "")),
            Annotation = doc.QuerySelector("div.card-description")?.InnerHtml
        };
            
        return book;
    }
    
    /// <summary>
    /// Авторизация в системе
    /// </summary>
    /// <exception cref="Exception"></exception>
    private async Task Authorize(){
        if (!_config.HasCredentials) {
            return;
        }

        var payload = new {
            email = _config.Login,
            password = _config.Password
        };
        
        using var post = await _config.Client.PostAsJsonAsync($"https://{HOST}/auth/login", payload);
        try {
            var data = await post.Content.ReadFromJsonAsync<AuthResponse>();
            if (!data.Success) {
                throw new Exception("Не удалось авторизоваться");
            }
        } catch {
            throw new Exception("Не удалось авторизоваться");
        }
    }

    private static string Normalize(string str) {
        return Regex.Replace(Regex.Replace(str, "\t|\n", " "), "\\s+", " ").Trim();
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.front img")?.Attributes["data-src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, new Uri(imagePath).AbsolutePath)) : Task.FromResult(default(Image));
    }

    private static List<Block> GetToc(Response response, string title) {
        var toc = JsonSerializer.Deserialize<List<Block>>(response.Toc);
        if (toc?.Count == 0) {
            toc = new List<Block> {
                new() {
                    Index = 0,
                    Chunk = new Chunk {
                        Mods = new[] {
                            new Mod {
                                Text = title
                            }
                        }
                    }
                }
            };
        }

        return toc;
    }

    private async Task<List<Chapter>> FillChapters(List<Block> toc, Block[] blocks, Uri bookUri, long eBookId, string title) {
        var result = new List<Chapter>();

        for (var i = 0; i < toc.Count; i++) {
            Console.WriteLine($"Загружаем главу {toc[i].Chunk.Mods[0].Text.Trim().CoverQuotes()}");
            var text = new StringBuilder();
            var chapter = new Chapter();

            foreach (var block in blocks.Where(b => b.Index >= toc[i].Index && (i == toc.Count -1 || b.Index < toc[i + 1].Index))) {
                var p = new StringBuilder();
                    
                foreach (var mod in block.Chunk.Mods) {
                    switch (mod.Type) {
                        case "IMAGE":
                            p.Append($"<img src='https://{HOST}/uploads/ebook/{eBookId}/{mod.Data.GetProperty("src").GetString()}' />");
                            break;
                        case "LINK":
                            p.Append($"<a href='{mod.Data.GetProperty("url").GetString()}'>{mod.Mods?.FirstOrDefault()?.Text ?? string.Empty}</a>");
                            break;
                        default:
                            p.Append($"{(mod.Text ?? string.Empty).Trim()} ");
                            break;
                    }
                }

                text.Append("<p>" + p + "</p>");
            }

            var chapterDoc = text.ToString().HtmlDecode().AsHtmlDoc();
            chapter.Images = await GetImages(chapterDoc, bookUri);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = toc[i].Chunk.Mods[0].Text.Trim();

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> Init(Uri uri) {
        _config.Client.DefaultRequestHeaders.Add("Host", "litmarket.ru");
        _config.Client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        
        var response = await _config.Client.GetWithTriesAsync(uri);
        var doc = await response.Content.ReadAsStringAsync().ContinueWith(t => t.Result.AsHtmlDoc());

        var csrf = doc.QuerySelector("[name=csrf-token]")?.Attributes["content"]?.Value;
        if (string.IsNullOrWhiteSpace(csrf)) {
            throw new ArgumentException("Не удалось получить csrf-token", nameof(csrf));
        }
            
        var cookies = response.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;
        var xsrfCookie = cookies.FirstOrDefault(c => c.StartsWith("XSRF-TOKEN="));
        if (xsrfCookie == null) {
            throw new ArgumentException("Не удалось получить XSRF-TOKEN", nameof(xsrfCookie));
        }
            
        var xsrf = HttpUtility.UrlDecode(xsrfCookie.Split(";")[0].Replace("XSRF-TOKEN=", ""));
            
        _config.Client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);
        _config.Client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", xsrf);

        return doc;
    }

    private async Task<Response> GetMainData(string bookId) {
        var data = await _config.Client.GetWithTriesAsync(new Uri($"https://{HOST}/reader/data/{bookId}"));
        return await data.Content.ReadFromJsonAsync<Response>();
    }

    private async Task<Block[]> GetBlocks(int eBookId) {
        var resp = await _config.Client.GetWithTriesAsync(new Uri($"https://{HOST}/reader/blocks/{eBookId}"));
        return await resp.Content.ReadFromJsonAsync<Block[]>();
    }
}