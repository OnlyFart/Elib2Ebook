using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using OnlineLib2Ebook.Configs;
using OnlineLib2Ebook.Extensions;
using OnlineLib2Ebook.Types.Book;
using OnlineLib2Ebook.Types.Ranobe;

namespace OnlineLib2Ebook.Logic.Getters {
    public class RanobeGetter : GetterBase {
        public RanobeGetter(BookGetterConfig config) : base(config) { }
        protected override Uri SystemUrl => new("https://ранобэ.рф/");
        
        protected override string GetId(Uri url) {
            var segments = url.Segments;
            return (segments.Length == 2 ? base.GetId(url) : segments[1]).Trim('/');
        }
        
        public override async Task<Book> Get(Uri url) {
            var bookId = GetId(url);
            var uri = new Uri($"https://ранобэ.рф/{bookId}");
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(uri);

            var ranobeBook = GetNextData<RanobeBook>(doc, "book");

            var book = new Book {
                Cover = await GetCover(ranobeBook, uri),
                Chapters = await FillChapters(ranobeBook, url),
                Title = ranobeBook.Title,
                Author = ranobeBook.Author?.Name ?? "Ranobe"
            };
            
            return book;
        }

        private async Task<IEnumerable<Chapter>> FillChapters(RanobeBook ranobeBook, Uri url) {
            var result = new List<Chapter>();
            
            foreach (var ranobeChapter in ranobeBook.Chapters.Reverse()) {
                Console.WriteLine($"Загружаем главу \"{ranobeChapter.Title}\"");
                var chapter = new Chapter();
                var ranobesChapter = await GetChapter(url, ranobeChapter.Url);
                var chapterDoc = HttpUtility.HtmlDecode(ranobesChapter.Content.Text).AsHtmlDoc();
                chapter.Images = await GetImages(chapterDoc, url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
                chapter.Title = ranobeChapter.Title;

                result.Add(chapter);
            }

            return result;
        }

        private async Task<RanobeChapter> GetChapter(Uri mainUrl, string url) {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri(mainUrl, url));
            return GetNextData<RanobeChapter>(doc, "chapter");
        }

        private static T GetNextData<T>(HtmlDocument doc, string node) {
            var json = doc.GetElementbyId("__NEXT_DATA__").InnerText;
            var bookProperty = JsonDocument.Parse(json)
                .RootElement.GetProperty("props")
                .GetProperty("pageProps")
                .GetProperty(node)
                .GetRawText();
            
            return JsonSerializer.Deserialize<T>(bookProperty);
        }
        
        private Task<Image> GetCover(RanobeBook book, Uri bookUri) {
            var imagePath = book.Images?.OrderByDescending(t => t.Height).FirstOrDefault()?.Url;
            return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
        }
    }
}