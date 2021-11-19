using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using OnlineLib2Ebook.Configs;
using OnlineLib2Ebook.Extensions;
using OnlineLib2Ebook.Types.Book;
using OnlineLib2Ebook.Types.Common;

namespace OnlineLib2Ebook.Logic.Getters {
    public class RulateGetter : GetterBase {
        public RulateGetter(BookGetterConfig config) : base(config) { }
        protected override Uri SystemUrl => new("https://tl.rulate.ru");

        protected override string GetId(Uri url) {
            var segments = url.Segments;
            return (segments.Length == 3 ? base.GetId(url) : segments[2]).Trim('/');
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
                Author = HttpUtility.HtmlDecode(GetAuthor(doc))
            };
            
            return book;
        }

        private string GetAuthor(HtmlDocument doc) {
            var info = doc.GetElementbyId("Info");
            const string AUTHOR = "rulate";
            foreach (var p in info.Descendants().Where(t => t.Name == "p")) {
                var strong = p.GetByFilter("strong");
                if (strong != null && strong.InnerText.Contains("Автор")) {
                    return p.GetTextByFilter("em") ?? AUTHOR;
                }
            }

            return AUTHOR;
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
            
            foreach (var (id, name) in GetChapters(doc)) {
                Console.WriteLine($"Загружаем главу \"{name}\"");
                var chapter = new Chapter();
                
                var text = await GetChapter(bookId, id);

                var chapterDoc = HttpUtility.HtmlDecode(text).AsHtmlDoc();
                chapter.Images = await GetImages(chapterDoc, bookUri);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
                chapter.Title = name;

                result.Add(chapter);
            }

            return result;
        }

        private async Task<string> GetChapter(string bookId, string chapterId) {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://tl.rulate.ru/book/{bookId}/{chapterId}/ready"));
            return doc.GetTextByFilter("h1") == "Доступ запрещен" ? string.Empty : doc.GetByFilter("div", "content-text")?.InnerHtml ?? string.Empty;
        }

        private IEnumerable<ChapterShort> GetChapters(HtmlDocument doc) {
            return doc.GetElementbyId("Chapters")
                .Descendants()
                .Where(t => t.Name == "tr")
                .Skip(1)
                .Where(chapter => chapter.Attributes.Contains("data-id"))
                .Select(chapter => new ChapterShort(chapter.Attributes["data-id"].Value, HttpUtility.HtmlDecode(chapter.GetTextByFilter("td", "t")).Trim()));
        }

        private async Task Mature(Uri url) {
            var data = new Dictionary<string, string> {
                { "path", url.LocalPath },
                { "ok", "Да" }
            };

            await _config.Client.PostAsync(new Uri($"https://tl.rulate.ru/mature?path={url.LocalPath}"), new FormUrlEncodedContent(data));
        }
    }
}