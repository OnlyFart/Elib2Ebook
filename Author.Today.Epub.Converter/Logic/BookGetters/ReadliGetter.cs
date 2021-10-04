using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Author.Today.Epub.Converter.Configs;
using Author.Today.Epub.Converter.Extensions;
using Author.Today.Epub.Converter.Types.Book;
using HtmlAgilityPack;

namespace Author.Today.Epub.Converter.Logic.BookGetters {
    public class ReadliGetter : GetterBase {
        public ReadliGetter(BookGetterConfig config) : base(config) { }
        public override Uri SystemUrl => new("https://readli.net");
        public override async Task<Book> Get(Uri url) {
            Init();
            var doc = await GetPage(url);

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
                var page = await GetPage(new Uri($"https://readli.net/chitat-online/?b={bookId}&pg={i}"));
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

        private async Task<HtmlDocument> GetPage(Uri uri) {
            return await _config.Client.GetStringAsync(uri).ContinueWith(t => t.Result.AsHtmlDoc());
        }
    }
}