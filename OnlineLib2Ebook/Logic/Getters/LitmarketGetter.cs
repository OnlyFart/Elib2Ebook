using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using OnlineLib2Ebook.Configs;
using OnlineLib2Ebook.Extensions;
using OnlineLib2Ebook.Types.Book;
using OnlineLib2Ebook.Types.Litmarket;

namespace OnlineLib2Ebook.Logic.Getters {
    public class LitmarketGetter : GetterBase {
        public LitmarketGetter(BookGetterConfig config) : base(config) { }
        protected override Uri SystemUrl => new("https://litmarket.ru");

        public override async Task<Book> Get(Uri url) {
            var bookId = GetId(url);
            url = new Uri($"https://litmarket.ru/books/{bookId}");
            var doc = await Init(url);

            var content = await GetMainData(bookId);
            var toc = JsonSerializer.Deserialize<List<Block>>(content.Toc);
            var blocks = await GetBlocks(content.Book.EbookId);

            var title = Normalize(doc.GetTextBySelector("h1.card-title"));
            var book = new Book {
                Cover = await GetCover(doc, url),
                Chapters = await FillChapters(toc, blocks, url, content.Book.EbookId, title),
                Title = title,
                Author = Normalize(doc.GetTextBySelector("div.card-author").Replace("Автор:", "")),
            };
            
            return book;
        }

        private static string Normalize(string str) {
            return HttpUtility.HtmlDecode(Regex.Replace(Regex.Replace(str, "\t|\n", " "), "\\s+", " ").Trim());
        }

        private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
            var imagePath = doc.QuerySelector("div.front img")?.Attributes["data-src"]?.Value;
            return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, imagePath)) : Task.FromResult(default(Image));
        }

        private async Task<List<Chapter>> FillChapters(List<Block> toc, Block[] blocks, Uri bookUri, long eBookId, string title) {
            var result = new List<Chapter>();

            if (toc.Count == 0) {
                toc.Add(new Block {
                    Index = 0,
                    Chunk = new Chunk {
                        Mods = new[]{new Mod {
                            Text = title
                        }}
                    }
                });
            }
            
            for (var i = 0; i < toc.Count; i++) {
                Console.WriteLine($"Загружаем главу \"{toc[i].Chunk.Mods[0].Text.Trim()}\"");
                var text = new StringBuilder();
                var chapter = new Chapter();

                foreach (var block in blocks.Where(b => b.Index >= toc[i].Index && (i == toc.Count -1 || b.Index < toc[i + 1].Index))) {
                    var p = new StringBuilder();
                    
                    foreach (var mod in block.Chunk.Mods) {
                        switch (mod.Type) {
                            case "IMAGE":
                                p.Append($"<img src='https://litmarket.ru/uploads/ebook/{eBookId}/{mod.Data.GetProperty("src").GetString()}' />");
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

                var chapterDoc = HttpUtility.HtmlDecode(text.ToString()).AsHtmlDoc();
                chapter.Images = await GetImages(chapterDoc, bookUri);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
                chapter.Title = toc[i].Chunk.Mods[0].Text.Trim();

                result.Add(chapter);
            }

            return result;
        }

        private async Task<HtmlDocument> Init(Uri uri) {
            var response = await _config.Client.GetStringWithTriesAsync(uri);
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
            var data = await _config.Client.GetStringWithTriesAsync(new Uri($"https://litmarket.ru/reader/data/{bookId}"));
            return await data.Content.ReadFromJsonAsync<Response>();
        }

        private async Task<Block[]> GetBlocks(int eBookId) {
            var resp = await _config.Client.GetStringWithTriesAsync(new Uri($"https://litmarket.ru/reader/blocks/{eBookId}"));
            return await resp.Content.ReadFromJsonAsync<Block[]>();
        }
    }
}