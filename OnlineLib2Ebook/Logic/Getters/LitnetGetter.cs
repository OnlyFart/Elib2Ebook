using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using OnlineLib2Ebook.Configs;
using OnlineLib2Ebook.Extensions;
using OnlineLib2Ebook.Types.Book;
using OnlineLib2Ebook.Types.Common;
using OnlineLib2Ebook.Types.Litnet.Response;

namespace OnlineLib2Ebook.Logic.Getters {
    public class LitnetGetter : GetterBase {
        public LitnetGetter(BookGetterConfig config) : base(config) { }

        protected override Uri SystemUrl => new("https://litnet.com/");

        public override async Task<Book> Get(Uri url) {
            var token = await GetToken();
            var bookId = GetId(url);
            var uri = new Uri($"https://litnet.com/ru/book/{bookId}");
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(uri);

            var title = HttpUtility.HtmlDecode(doc.GetTextByFilter("h1", "roboto"));
            
            var book = new Book {
                Cover = await GetCover(doc, uri),
                Chapters = await FillChapters(doc, uri, title, bookId, token),
                Title = title,
                Author = HttpUtility.HtmlDecode(doc.GetTextByFilter("a", "author"))
            };
            
            return book;
        }
        
        private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
            var imagePath = doc.GetByFilter("div", "book-view-cover")
                ?.GetByFilter("img")
                ?.Attributes["src"]?.Value;

            return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
        }


        private async Task<List<Chapter>> FillChapters(HtmlDocument doc, Uri bookUri, string title, string bookId, string token) {
            var result = new List<Chapter>();
            
            foreach (var litnetChapter in await GetChapters(doc, bookId, title)) {
                Console.WriteLine($"Загружаем главу \"{litnetChapter.Name}\"");
                var text = new StringBuilder();
                var chapter = new Chapter();
                
                for (var i = 1;; i++) {
                    var page = await GetPage(litnetChapter, i, token);
                    if (page.Status == 0) {
                        break;
                    }

                    var pageDoc = page.Data
                        .AsHtmlDoc()
                        .RemoveNodes(t => t.Name != "p" && t.Name != "#text");
                    
                    text.Append(pageDoc.DocumentNode.InnerHtml);
                    if (page.IsLastPage) {
                        break;
                    }
                }
                
                var chapterDoc = HttpUtility.HtmlDecode(text.ToString()).AsHtmlDoc();
                chapter.Images = await GetImages(chapterDoc, bookUri);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
                chapter.Title = litnetChapter.Name;

                result.Add(chapter);
            }

            return result;
        }

        private async Task<LitnetResponse> GetPage(ChapterShort chapter, int page, string token) {
            var data = new Dictionary<string, string> {
                ["chapterId"] = chapter.Id,
                ["page"] = page.ToString(),
                ["_csrf"] = token
            };
            
            Console.WriteLine($"Загружаем страницу {page} главы \"{chapter.Name}\"");
            for (var i = 0; i < 5; i++) {
                var resp = await _config.Client.PostAsync("https://litnet.com/reader/get-page", new FormUrlEncodedContent(data));
                if (resp.StatusCode == HttpStatusCode.TooManyRequests) {
                    return new LitnetResponse {
                        Status = 0
                    };
                }
                
                
                if (resp.StatusCode != HttpStatusCode.OK) {
                    await Task.Delay(5000);
                    continue;
                }

                await Task.Delay(1000);
                return await resp.Content.ReadFromJsonAsync<LitnetResponse>();
            }

            return new LitnetResponse();
        }

        private async Task<IEnumerable<ChapterShort>> GetChapters(HtmlDocument doc, string bookId, string title) {
            var result = doc
                .DocumentNode
                .Descendants()
                .Where(t => t.Name == "option" && !string.IsNullOrWhiteSpace(t.Attributes["value"].Value))
                .Select(option => new ChapterShort(option.Attributes["value"].Value, option.InnerText)).ToList();

            if (result.Count > 0) {
                return result;
            }

            var readerPage = await _config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://litnet.com/ru/reader/{bookId}"));
            var chapter = readerPage.DocumentNode.Descendants().FirstOrDefault(t => t.Name == "div" && t.Attributes["data-chapter"] != null);
            if (chapter == null) {
                return result;
            }

            return new[] { new ChapterShort(chapter.Attributes["data-chapter"].Value, title) };
        }

        private async Task<string> GetToken() {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri("https://litnet.com/auth/login?classic=1&link=https://litnet.com/"));
            return doc.GetAttributeByNameAttribute("_csrf", "value");
        }
    }
}