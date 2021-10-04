using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Author.Today.Epub.Converter.Configs;
using Author.Today.Epub.Converter.Extensions;
using Author.Today.Epub.Converter.Types.Book;
using Author.Today.Epub.Converter.Types.Litmarket;
using HtmlAgilityPack;

namespace Author.Today.Epub.Converter.Logic.BookGetters {
    public class LitmarketGetter : GetterBase {
        public LitmarketGetter(BookGetterConfig config) : base(config) { }
        public override Uri SystemUrl => new("https://litmarket.ru");

        public override async Task<Book> Get(Uri url) {
            var doc = await Init(url);
            var bookId = GetId(url);

            var content = await GetMainData(bookId);
            var toc = JsonSerializer.Deserialize<Block[]>(content.Toc);
            var blocks = await GetBlocks(content.Book.EbookId);

            var book = new Book(bookId) {
                Cover = await GetCover(doc, url),
                Chapters = await FillChapters(toc, blocks, url),
                Title = Normalize(doc.GetTextByFilter("h1", "card-title")),
                Author = Normalize(doc.GetTextByFilter("div", "card-author").Replace("Автор:", "")),
            };
            
            return book;
        }

        private static string Normalize(string str) {
            return HttpUtility.HtmlDecode(Regex.Replace(Regex.Replace(str, "\t|\n", " "), "\\s+", " ").Trim());
        }

        private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
            var imagePath = doc.DocumentNode.Descendants()
                .FirstOrDefault(t => t.Name == "div" && t.Attributes["class"]?.Value == "front")
                ?.Descendants()
                ?.FirstOrDefault(t => t.Name == "img")
                ?.Attributes["data-src"]?.Value;

            return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, imagePath)) : Task.FromResult(default(Image));
        }

        private async Task<List<Chapter>> FillChapters(Block[] toc, Block[] blocks, Uri bookUri) {
            var result = new List<Chapter>();
            
            foreach (var t in toc) {
                var nextChapter = 0;
                var text = new StringBuilder();
                var chapter = new Chapter();
                
                foreach (var block in blocks.SkipWhile(b => b.Chunk.Key != t.Chunk.Key)) {
                    if (block.Chunk.Type == "chapter") {
                        nextChapter++;
                    }

                    if (nextChapter > 1) {
                        break;
                    }

                    foreach (var mod in block.Chunk.Mods) {
                        text.AppendLine($"<p>{(mod.Text ?? string.Empty).Trim()}</p>");
                    }
                }
                
                var chapterDoc = HttpUtility.HtmlDecode(text.ToString()).AsHtmlDoc();
                chapter.Images = await GetImages(chapterDoc, bookUri);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
                chapter.Title = t.Chunk.Mods[0].Text.Trim();

                result.Add(chapter); 
            }

            return result;
        }

        private async Task<HtmlDocument> Init(Uri uri) {
            var response = await _config.Client.GetStringWithTriesAsync(uri);
            var doc = await response.Content.ReadAsStringAsync().ContinueWith(t => t.Result.AsHtmlDoc());

            var csrf = doc.GetAttributeByNameAttribute("csrf-token", "content");
            if (string.IsNullOrWhiteSpace(csrf)) {
                throw new ArgumentException("Не удалось получить csrf-token", nameof(csrf));
            }
            
            var cookies = response.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;
            var xsrfCookie = cookies.FirstOrDefault(c => c.StartsWith("XSRF-TOKEN="));
            if (xsrfCookie == null) {
                throw new ArgumentException("Не удалость получить XSRF-TOKEN", nameof(xsrfCookie));
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