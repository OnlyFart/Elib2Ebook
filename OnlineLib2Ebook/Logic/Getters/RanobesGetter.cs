using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
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

            var book = new Book {
                Cover = await GetCover(doc, uri),
                Chapters = await FillChapters(doc, uri),
                Title = HttpUtility.HtmlDecode(doc.QuerySelector("h1.title").FirstChild.InnerText.Trim()),
                Author = "Ранобэс"
            };
            
            return book;
        }

        private async Task<Uri> GetMainUrl(Uri url) {
            if (url.Segments[1] == "chapters/") {
                var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
                return new Uri(url, doc.QuerySelector("div.category a").Attributes["href"].Value);
            }

            return url;
        }

        private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
            var result = new List<Chapter>();

            foreach (var ranobeChapter in await GetChapters(GetTocLink(doc, url))) {
                Console.WriteLine($"Загружаем главу \"{ranobeChapter.Title}\"");
                var chapter = new Chapter();
                var chapterDoc = HttpUtility.HtmlDecode(await GetChapter(url, ranobeChapter.Url))
                    .AsHtmlDoc()
                    .RemoveNodes(t => t.Name is "script" or "br" || t.Id?.Contains("yandex_rtb") == true);
                
                chapter.Images = await GetImages(chapterDoc, url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
                chapter.Title = ranobeChapter.Title;

                result.Add(chapter);
            }

            return result;
        }

        private async Task<string> GetChapter(Uri mainUrl, string url) {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri(mainUrl, url));
            var sb = new StringBuilder();
            foreach (var node in doc.QuerySelectorAll("#arrticle > :not(.splitnewsnavigation)")) {
                var tag = node.Name == "#text" ? "p" : node.Name;
                sb.AppendLine($"<{tag}>{node.InnerHtml.Trim()}</{tag}>");
            }
            
            return sb.ToString();
        }

        private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
            var imagePath = doc.QuerySelector("div.poster img")?.Attributes["src"]?.Value;
            return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
        }

        private Uri GetTocLink(HtmlDocument doc, Uri uri) {
            var relativeUri = doc.QuerySelector("div.r-fullstory-chapters-foot a[title~=оглавление]").Attributes["href"].Value;
            return new Uri(uri, relativeUri);
        }
        
        private async Task<IEnumerable<RanobesChapter>> GetChapters(Uri tocUri) {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(tocUri);
            var lastA = doc.QuerySelector("div.pages a:last-child")?.InnerText;
            var pages = string.IsNullOrWhiteSpace(lastA) ? 1 : int.Parse(lastA);
            
            Console.WriteLine("Получаем оглавление");
            var chapters = new List<RanobesChapter>();
            for (var i = 1; i <= pages; i++) {
                doc = await _config.Client.GetHtmlDocWithTriesAsync(new Uri(tocUri.AbsoluteUri + "/page/" + i));
                chapters.AddRange(doc
                    .QuerySelectorAll("#dle-content > .cat_block.cat_line a")
                    .Select(a => new RanobesChapter(a.Attributes["title"].Value, a.Attributes["href"].Value)));
            }
            Console.WriteLine($"Получено {chapters.Count} глав");

            chapters.Reverse();
            return chapters;
        }
    }
}