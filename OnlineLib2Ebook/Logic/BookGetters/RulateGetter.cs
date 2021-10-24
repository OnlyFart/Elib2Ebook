using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using OnlineLib2Ebook.Configs;
using OnlineLib2Ebook.Extensions;
using OnlineLib2Ebook.Types.Book;
using OnlineLib2Ebook.Types.Common;

namespace OnlineLib2Ebook.Logic.BookGetters {
    public class RulateGetter : GetterBase {
        public RulateGetter(BookGetterConfig config) : base(config) { }
        public override Uri SystemUrl => new("https://tl.rulate.ru");

        public override string GetId(Uri url) {
            var segments = url.Segments;
            return segments.Length == 3 ? base.GetId(url) : segments[2].Trim('/');
        }

        public override async Task<Book> Get(Uri url) {
            var bookId = GetId(url);
            url = new Uri($"https://tl.rulate.ru/book/{bookId}");
            await Mature(url);
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
            
            
            var book = new Book(bookId) {
                Cover = await GetCover(doc, url),
                Chapters = await FillChapters(doc, url, bookId),
                Title = HttpUtility.HtmlDecode(doc.GetTextByFilter("h1")),
                Author = HttpUtility.HtmlDecode("rulate")
            };
            return book;
        }
        
        private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
            var imagePath = doc.GetByFilter("div", "slick")
                ?.Descendants()
                ?.FirstOrDefault(t => t.Name == "img")
                ?.Attributes["src"]?.Value;

            return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
        }
        
        private async Task<List<Chapter>> FillChapters(HtmlDocument doc, Uri bookUri, string bookId) {
            var result = new List<Chapter>();
            
            foreach (var chapterShort in GetChapters(doc)) {
                Console.WriteLine($"Загружаем главу \"{chapterShort.Name}\"");
                var chapter = new Chapter();
                
                var text = await GetChapter(bookId, chapterShort.Id);

                var chapterDoc = HttpUtility.HtmlDecode(text).AsHtmlDoc();
                chapter.Images = await GetImages(chapterDoc, bookUri);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
                chapter.Title = chapterShort.Name;

                result.Add(chapter);
            }

            return result;
        }

        private async Task<string> GetChapter(string bookId, string chapterId) {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://tl.rulate.ru/book/{bookId}/{chapterId}/ready"));

            var h1 = doc.GetTextByFilter("h1");
            if (h1 == "Доступ запрещен") {
                return string.Empty;
            }

            return doc.GetByFilter("div", "content-text").InnerHtml;

        }

        private IEnumerable<ChapterShort> GetChapters(HtmlDocument doc) {
            var chapters = doc.GetElementbyId("Chapters");
            foreach (var chapter in chapters.Descendants().Where(t => t.Name == "tr").Skip(1)) {
                if (chapter.Attributes.Contains("data-id")) {
                    yield return new ChapterShort(chapter.Attributes["data-id"].Value, HttpUtility.HtmlDecode(chapter.GetTextByFilter("td", "t")).Trim());
                }
            }
        }

        private async Task Mature(Uri url){
            var data = new Dictionary<string, string>() {
                { "path", url.LocalPath },
                { "ok", "Да" }
            };

            await _config.Client.PostAsync(new Uri($"https://tl.rulate.ru/mature?path={url.LocalPath}"), new FormUrlEncodedContent(data));
        }
    }
}