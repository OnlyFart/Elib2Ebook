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

namespace OnlineLib2Ebook.Logic.BookGetters {
    public class ReadliGetter : GetterBase {
        public ReadliGetter(BookGetterConfig config) : base(config) { }
        public override Uri SystemUrl => new("https://readli.net");
        public override async Task<Book> Get(Uri url) {
            Init();
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
            var lastSegment = GetLastSegment(url);
            
            // Находимся на странице ридера
            if (lastSegment.StartsWith("chitat-online", StringComparison.InvariantCultureIgnoreCase)) {
                url = GetMainUrl(url, doc); 
                doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
            }

            var pages = long.Parse(doc.GetTextByFilter("span", "button-pages__right").Split(' ')[0]);
            var imageDiv = doc.GetByFilter("div", "book-image");
            var href = new Uri(url, imageDiv.Descendants().FirstOrDefault(t => t.Name == "a").Attributes["href"].Value);
            var bookId = GetBookId(href);
            
            var name = doc.GetTextByFilter("h1", "main-info__title");
            var author = doc.GetTextByFilter("a", "main-info__link");
            
            var book = new Book(bookId.ToString()) {
                Cover = await GetCover(imageDiv, url),
                Chapters = await FillChapters(bookId, pages, name),
                Title = name,
                Author = author
            };
            
            return book; 
        }

        private long GetBookId(Uri uri) {
            return long.Parse(uri.Query.Trim('?').Split("&").FirstOrDefault(p => p.StartsWith("b=")).Replace("b=", ""));
        }

        private string GetLastSegment(Uri uri) {
            return uri.Segments.Last();
        }

        private Uri GetMainUrl(Uri url, HtmlDocument doc) {
            var href = doc.DocumentNode.Descendants()
                .FirstOrDefault(t => t.Name == "h1")
                .Descendants()
                .FirstOrDefault(t => t.Name == "a")
                .Attributes["href"]
                .Value;

            return new Uri(url, href);
        }

        private void Init() {
            _config.Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.0 Safari/605.1.15");
            _config.Client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            _config.Client.DefaultRequestHeaders.Add("Accept-Language", "ru");
            _config.Client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        }
        
        private Task<Image> GetCover(HtmlNode doc, Uri bookUri) {
            var imagePath = doc.Descendants()
                ?.FirstOrDefault(t => t.Name == "img")
                ?.Attributes["src"]?.Value;

            return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
        }

        private async Task<List<Chapter>> FillChapters(long bookId, long pages, string name) {
            var chapter = new Chapter();
            var text = new StringBuilder();
            
            for (var i = 1; i <= pages; i++) {
                Console.WriteLine($"Получаю страницу {i}/{pages}");
                var page = await _config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://readli.net/chitat-online/?b={bookId}&pg={i}"));
                var content = page.GetByFilter("div", "reading__text");

                foreach (var node in content.ChildNodes) {
                    if (node.Name is "h3" or "p") {
                        text.AppendFormat($"<p>{node.InnerText}</p>");
                    }
                }
            }
            
            var chapterDoc = HttpUtility.HtmlDecode(text.ToString()).AsHtmlDoc();
            chapter.Images = await GetImages(chapterDoc, new Uri("https://readli.net/chitat-online/"));
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = name;
            
            return new List<Chapter>{ chapter };
        }
    }
}