using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using OnlineLib2Ebook.Configs;
using OnlineLib2Ebook.Extensions;
using OnlineLib2Ebook.Types.Book;
using OnlineLib2Ebook.Types.RanobeLib;
using Chapter = OnlineLib2Ebook.Types.Book.Chapter;

namespace OnlineLib2Ebook.Logic.Getters {
    public class RanobeLibGetter : GetterBase {
        public RanobeLibGetter(BookGetterConfig config) : base(config) { }
        protected override Uri SystemUrl => new("https://ranobelib.me");

        protected override string GetId(Uri url) {
            return url.Segments[1].Trim('/');
        }

        public override async Task<Book> Get(Uri url) {
            Init();
            var bookId = GetId(url);
            var uri = new Uri($"https://staticlib.me/{bookId}");
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(uri);

            var data = GetData(doc);

            var book = new Book {
                Cover = await GetCover(doc, uri),
                Chapters = await FillChapters(data, uri),
                Title = doc.QuerySelector("meta[property=og:title]").Attributes["content"].Value.Trim(),
                Author = "RanobeLib"
            };
            
            return book;
        }

        private WindowData GetData(HtmlDocument doc) {
            var match = new Regex("window.__DATA__ = (?<data>{.*}).*window._SITE_COLOR_", RegexOptions.Compiled | RegexOptions.Singleline).Match(doc.Text).Groups["data"].Value;
            var windowData = JsonSerializer.Deserialize<WindowData>(match);
            windowData.Chapters.List.Reverse();
            return windowData;
        }

        private async Task<IEnumerable<Chapter>> FillChapters(WindowData data, Uri url) {
            var result = new List<Chapter>();
            
            foreach (var ranobeChapter in data.Chapters.List) {
                Console.WriteLine($"Загружаем главу {ranobeChapter.GetName()}");
                var chapter = new Chapter();
                var chapterDoc = await _config.Client.GetHtmlDocWithTriesAsync(ranobeChapter.GetUri(url));
                chapter.Images = await GetImages(chapterDoc, ranobeChapter.GetUri(url));
                chapter.Content = chapterDoc.QuerySelector("div.reader-container").InnerHtml;
                chapter.Title = ranobeChapter.GetName();

                result.Add(chapter);
            }

            return result;
        }

        private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
            var imagePath = doc.QuerySelector("meta[property=og:image]").Attributes["content"].Value.Trim();
            return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(uri, imagePath)) : Task.FromResult(default(Image));
        }
    }
}