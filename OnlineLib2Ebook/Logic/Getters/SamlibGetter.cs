using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using OnlineLib2Ebook.Configs;
using OnlineLib2Ebook.Extensions;
using OnlineLib2Ebook.Types.Book;

namespace OnlineLib2Ebook.Logic.Getters {
    public class SamlibGetter : GetterBase {
        private const string START_PATTERN = "<!----------- Собственно произведение --------------->";
        public const string END_PATTERN = "<!--------------------------------------------------->";
        
        public SamlibGetter(BookGetterConfig config) : base(config) { }
        protected override Uri SystemUrl => new("http://samlib.ru/");
        public override async Task<Book> Get(Uri url) {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);

            var title = HttpUtility.HtmlDecode(doc.GetTextByFilter("h2"));
            var book = new Book {
                Cover = null,
                Chapters = await FillChapters(doc, url, title),
                Title = title,
                Author = "Samlib"
            };
            
            return book;
        }

        private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url, string title) {
            var chapter = new Chapter();
            
            var start = doc.Text.IndexOf(START_PATTERN, StringComparison.InvariantCultureIgnoreCase) + START_PATTERN.Length + 1;
            var stop = doc.Text.IndexOf(END_PATTERN, start, StringComparison.InvariantCultureIgnoreCase) - END_PATTERN.Length - 1;

            doc.LoadHtml(doc.Text[start..stop]);
            
            var sr = new StringReader(HttpUtility.HtmlDecode(doc.DocumentNode.InnerText));
            var text = new StringBuilder();
            while (true) {
                var line = await sr.ReadLineAsync();
                if (line == null) {
                    break;
                }
                
                if (!string.IsNullOrWhiteSpace(line)) {
                    text.AppendLine($"<p>{HttpUtility.HtmlEncode(line.Trim())}</p>");
                }
            }
            

            var chapterDoc = HttpUtility.HtmlDecode(text.ToString()).AsHtmlDoc();
            chapter.Images = await GetImages(chapterDoc, url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = title;
            
            return new List<Chapter>{ chapter };
        }
    }
}