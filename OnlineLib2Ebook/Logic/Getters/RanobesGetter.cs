using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using OnlineLib2Ebook.Configs;
using OnlineLib2Ebook.Extensions;
using OnlineLib2Ebook.Types.Book;
using OnlineLib2Ebook.Types.Ranobes;

namespace OnlineLib2Ebook.Logic.Getters {
    public class RanobesGetter : GetterBase {
        public RanobesGetter(BookGetterConfig config) : base(config) { }
        protected override Uri SystemUrl => new("https://ranobes.com");

        protected override string GetId(Uri url) {
            return base.GetId(url).Split(".")[0];
        }

        public override async Task<Book> Get(Uri url) {
            Init();
            url = await GetMainUrl(url);
            var bookId = GetId(url);
            var uri = new Uri($"https://ranobes.com/ranobe/{bookId}.html");
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(uri);

            var book = new Book(bookId) {
                Cover = await GetCover(doc, uri),
                Chapters = await FillChapters(doc, uri),
                Title = HttpUtility.HtmlDecode(doc.GetByFilter("h1", "title").FirstChild.InnerText.Trim()),
                Author = "Ранобэс"
            };
            
            return book;
        }

        private async Task<Uri> GetMainUrl(Uri url) {
            if (url.Segments[1] == "chapters/") {
                var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
                var div = doc.DocumentNode.GetByFilterContains("div", "category");
                return new Uri(url, div.Descendants().FirstOrDefault(t => t.Name == "a").Attributes["href"].Value);
            }

            return url;
        }

        private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
            var result = new List<Chapter>();

            foreach (var ranobeChapter in await GetChapters(GetTocLink(doc, url))) {
                Console.WriteLine($"Загружаем главу \"{ranobeChapter.Title}\"");
                var chapter = new Chapter();
                var chapterDoc = ClearHtml(HttpUtility.HtmlDecode(await GetChapter(url, ranobeChapter.Url)).AsHtmlDoc());
                chapter.Images = await GetImages(chapterDoc, url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
                chapter.Title = ranobeChapter.Title;

                result.Add(chapter);
            }

            return result;
        }

        private static HtmlDocument ClearHtml(HtmlDocument doc) {
            var toRemove = doc.DocumentNode.Descendants().Where(t => t.Name is "script" or "br" || t.Id?.Contains("yandex_rtb") == true).ToList();
            foreach (var node in toRemove) {
                node.Remove();
            }

            return doc;
        }
        
        private async Task<string> GetChapter(Uri mainUrl, string url) {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri(mainUrl, url));
            var article = doc.GetElementbyId("arrticle");
            var sb = new StringBuilder();
            foreach (var node in article.ChildNodes) {
                if (node.Attributes["class"]?.Value?.Contains("splitnewsnavigation") == null) {
                    var tag = node.Name == "#text" ? "p" : node.Name;
                    sb.AppendLine($"<{tag}>{node.InnerHtml.Trim()}</{tag}>");
                }
            }
            
            return sb.ToString();
        }

        private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
            var imagePath = doc.GetByFilter("div", "poster")
                ?.Descendants()
                ?.FirstOrDefault(t => t.Name == "img")
                ?.Attributes["src"]?.Value;

            return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
        }

        private Uri GetTocLink(HtmlDocument doc, Uri uri) {
            var div = doc.GetByFilter("div", "r-fullstory-chapters-foot");
            return new Uri(uri, div.Descendants().LastOrDefault(t => t.Name == "a" && t.Attributes["title"]?.Value == "Перейти в оглавление").Attributes["href"].Value);
        }
        
        private async Task<IEnumerable<RanobesChapter>> GetChapters(Uri tocUri) {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(tocUri);
            var lastA = doc.GetByFilter("div", "pages")?.Descendants().LastOrDefault(t => t.Name == "a")?.InnerText;
            var pages = string.IsNullOrWhiteSpace(lastA) ? 1 : int.Parse(lastA);
            
            Console.WriteLine("Получаем оглавление");
            var chapters = new List<RanobesChapter>();
            for (var i = 1; i <= pages; i++) {
                doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri(tocUri.AbsoluteUri + "/page/" + i));
                chapters.AddRange(doc
                    .GetElementbyId("dle-content")
                    .ChildNodes
                    .Where(child => child.Attributes["class"]?.Value == "cat_block cat_line")
                    .Select(child => child.GetByFilter("a"))
                    .Where(a => a != null)
                    .Select(a => new RanobesChapter(a.Attributes["title"].Value, a.Attributes["href"].Value)));
            }
            Console.WriteLine($"Получено {chapters.Count} глав");

            chapters.Reverse();
            return chapters;
        }
    }
}