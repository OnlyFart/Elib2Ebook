using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using OnlineLib2Ebook.Configs;
using OnlineLib2Ebook.Extensions;
using OnlineLib2Ebook.Types.Book;
using OnlineLib2Ebook.Types.Common;

namespace OnlineLib2Ebook.Logic.Getters {
    public class FicbookGetter : GetterBase {
        public FicbookGetter(BookGetterConfig config) : base(config) { }
        protected override Uri SystemUrl => new("https://ficbook.net/");

        protected override string GetId(Uri url) {
            return url.Segments[2].Trim('/');
        }

        public override async Task<Book> Get(Uri url) {
            Init();
            var bookId = GetId(url);
            var uri = new Uri($"https://ficbook.net/readfic/{bookId}");
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(uri);

            var book = new Book {
                Cover = null,
                Chapters = await FillChapters(doc, url),
                Title = doc.GetTextBySelector("h1.mb-10"),
                Author = "Ficbook"
            };
            
            return book;
        }

        private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
            var result = new List<Chapter>();
            
            foreach (var ficbookChapter in GetChapters(doc, url)) {
                Console.WriteLine($"Загружаем главу \"{ficbookChapter.Title}\"");
                var chapter = new Chapter();
                var chapterDoc = await GetChapter(ficbookChapter);
                chapter.Images = await GetImages(chapterDoc, url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
                chapter.Title = ficbookChapter.Title;

                result.Add(chapter);
            }

            return result;
        }

        private async Task<HtmlDocument> GetChapter(UrlChapter urlChapter) {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url);
            var content = doc.QuerySelector("#content").RemoveNodes(n => n.Name == "div");
            using var sr = new StringReader(content.InnerText.HtmlDecode());

            var text = new StringBuilder();
            while (true) {
                var line = await sr.ReadLineAsync();
                if (line == null) {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                
                text.AppendLine($"<p>{line.HtmlEncode()}</p>");
            }

            return text.ToString().HtmlDecode().AsHtmlDoc();
        }

        private static IEnumerable<UrlChapter> GetChapters(HtmlDocument doc, Uri url) {
            foreach (var li in doc.QuerySelectorAll("li.part")) {
                var a = li.QuerySelector("a.part-link.visit-link");
                if (a != null) {
                    yield return new UrlChapter(new Uri(url, a.Attributes["href"].Value), li.GetTextBySelector("h3"));
                }
            }
        }
    }
}