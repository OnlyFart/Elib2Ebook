using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Litmarket;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class LitmarketGetter : GetterBase {
    public LitmarketGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://litmarket.ru");
    
    // cloudflare :(
    private readonly Uri _host = new("https://84.201.161.210/");

    public override async Task Init() {
        await base.Init();
        
        Config.Client.DefaultRequestHeaders.Add("Host", SystemUrl.Host);
        Config.Client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        
        var response = await Config.Client.GetWithTriesAsync(_host);
        var doc = await response.Content.ReadAsStreamAsync().ContinueWith(t => t.Result.AsHtmlDoc());

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
            
        Config.Client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);
        Config.Client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", xsrf);
    }

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = _host.MakeRelativeUri($"/books/{bookId}");

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        var content = await GetMainData(bookId);
            
        var blocks = await GetBlocks(content.Book.EbookId);

        var title = doc.GetTextBySelector("h1.card-title").ReplaceNewLine();
        var book = new Book(url.ReplaceHost(SystemUrl.Host)) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(GetToc(content, title), blocks, url, content.Book.EbookId),
            Title = title,
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div.card-description")?.InnerHtml,
            Seria = GetSeria(doc, url)
        };
            
        return book;
    }
    
    private static Seria GetSeria(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("div.card-cycle a");
        if (a == default || !a.GetText().Contains('#')) {
            return default;
        }
        
        var parts = a.GetText().Split('#', StringSplitOptions.RemoveEmptyEntries);

        return new Seria {
            Name = parts[0].HtmlDecode(),
            Number = parts[1].HtmlDecode(),
            Url = url.MakeRelativeUri(a.Attributes["href"].Value)
        };
    }

    private Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("div.card-author a");
        return a == default ? 
            new Author(doc.GetTextBySelector("div.card-author").Replace("Автор:", "").ReplaceNewLine()): 
            new Author(a.GetText().ReplaceNewLine(), url.MakeRelativeUri(a.Attributes["href"].Value).ReplaceHost(SystemUrl.Host));
    }
    
    /// <summary>
    /// Авторизация в системе
    /// </summary>
    /// <exception cref="Exception"></exception>
    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }

        var payload = new {
            email = Config.Options.Login,
            password = Config.Options.Password
        };
        
        using var post = await Config.Client.PostAsJsonAsync(_host.MakeRelativeUri("/auth/login"), payload);
        try {
            var data = await post.Content.ReadFromJsonAsync<AuthResponse>();
            if (data is { Success: false }) {
                throw new Exception("Не удалось авторизоваться");
            }
        } catch {
            throw new Exception("Не удалось авторизоваться");
        }
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.front img")?.Attributes["data-src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath.AsUri().AbsolutePath)) : Task.FromResult(default(Image));
    }

    private List<Block> GetToc(Response response, string title) {
        var toc = response.Toc.Deserialize<List<Block>>();
        if (toc?.Count == 0) {
            toc = [
                new() {
                    Index = 0,
                    Chunk = new Chunk {
                        Mods = [
                            new Mod {
                                Text = title
                            }
                        ]
                    }
                }
            ];
        }

        return SliceToc(toc, c => c.Chunk.Mods[0].Text).ToList();
    }

    private async Task<List<Chapter>> FillChapters(List<Block> toc, Block[] blocks, Uri bookUri, long eBookId) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        for (var i = 0; i < toc.Count; i++) {
            var chapterTitle = string.IsNullOrWhiteSpace(toc[i].Chunk.Mods[0].Text.Trim()) ? "Без названия" : toc[i].Chunk.Mods[0].Text.Trim();
            Config.Logger.LogInformation($"Загружаю главу {chapterTitle.CoverQuotes()}");
            var sb = new StringBuilder();
            var chapter = new Chapter();

            foreach (var block in blocks.Where(b => b.Index >= toc[i].Index && (i == toc.Count -1 || b.Index < toc[i + 1].Index))) {
                var p = new StringBuilder();
                    
                foreach (var mod in block.Chunk.Mods) {
                    switch (mod.Type) {
                        case "IMAGE":
                            p.Append($"<img src='https://{_host.Host}/uploads/ebook/{eBookId}/{mod.Data.GetProperty("src").GetString()}' />");
                            break;
                        case "LINK":
                            p.Append($"<a href='{mod.Data.GetProperty("url").GetString()}'>{mod.Mods?.FirstOrDefault()?.Text ?? string.Empty}</a>");
                            break;
                        default:
                            var text = $"{mod.Text ?? string.Empty}";
                            if (mod.Styles?.Length > 0) {
                                foreach (var style in mod.Styles) {
                                    switch (style) {
                                        case "ITALIC":
                                            text = text.CoverTag("em");
                                            break;
                                        case "BOLD":
                                            text = text.CoverTag("b");
                                            break;
                                        case "UNDERLINE":
                                            text = text.CoverTag("u");
                                            break;
                                        default:
                                            Config.Logger.LogInformation(style);
                                            break;
                                    }
                                }
                            }

                            p.Append(text);
                            break;
                    }
                }

                sb.Append(p.ToString().CoverTag("p"));
            }

            var chapterDoc = sb.AsHtmlDoc();
            chapter.Images = await GetImages(chapterDoc, bookUri);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = chapterTitle;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<Response> GetMainData(string bookId) {
        var data = await Config.Client.GetWithTriesAsync(_host.MakeRelativeUri($"/reader/data/{bookId}"));
        return await data.Content.ReadFromJsonAsync<Response>();
    }

    private async Task<Block[]> GetBlocks(int eBookId) {
        var resp = await Config.Client.GetWithTriesAsync(_host.MakeRelativeUri($"/reader/blocks/{eBookId}"));
        return await resp.Content.ReadFromJsonAsync<Block[]>();
    }
}