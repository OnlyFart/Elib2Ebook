using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
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

            var book = new Book {
                Cover = await GetCover(doc, url),
                Chapters = await FillChapters(doc, url, bookId),
                Title = doc.GetTextBySelector("h1"),
                Author = GetAuthor(doc)
            };
            
            return book;
        }

        private static string GetAuthor(HtmlDocument doc) {
            const string AUTHOR = "rulate";
            foreach (var p in doc.QuerySelectorAll("#Info p")) {
                var strong = p.QuerySelector("strong");
                if (strong != null && strong.InnerText.Contains("Автор")) {
                    return p.GetTextBySelector("em") ?? AUTHOR;
                }
            }

            return AUTHOR;
        }
        
        private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
            var imagePath = doc.QuerySelector("div.slick img")?.Attributes["src"]?.Value;
            return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
        }
        
        private async Task<List<Chapter>> FillChapters(HtmlDocument doc, Uri bookUri, string bookId) {
            var result = new List<Chapter>();
            
            foreach (var (id, name) in GetChapters(doc)) {
                Console.WriteLine($"Загружаем главу {name.CoverQuotes()}");
                var chapter = new Chapter();
                
                var text = await GetChapter(bookId, id);

                var chapterDoc = text.AsHtmlDoc();
                chapter.Images = await GetImages(chapterDoc, bookUri);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
                chapter.Title = name;

                result.Add(chapter);
            }

            return result;
        }

        private async Task<string> GetChapter(string bookId, string chapterId) {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://tl.rulate.ru/book/{bookId}/{chapterId}/ready"));
            return doc.GetTextBySelector("h1") == "Доступ запрещен" ? string.Empty : doc.QuerySelector("div.content-text")?.InnerHtml ?? string.Empty;
        }

        private static IEnumerable<IdChapter> GetChapters(HtmlDocument doc) {
            return doc.QuerySelectorAll("#Chapters tr[data-id]")
                .Select(chapter => new IdChapter(chapter.Attributes["data-id"].Value, chapter.GetTextBySelector("td.t")));
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