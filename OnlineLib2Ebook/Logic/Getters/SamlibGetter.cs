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

namespace OnlineLib2Ebook.Logic.Getters {
    public class SamlibGetter : GetterBase {
        private const string START_PATTERN = "Собственно произведение";
        private const string END_PATTERN = "-----------------------------------------------";
        
        public SamlibGetter(BookGetterConfig config) : base(config) { }
        protected override Uri SystemUrl => new("http://samlib.ru/");
        public override async Task<Book> Get(Uri url) {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);

            var title = doc.GetTextBySelector("h2").HtmlDecode();
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
            
            // Мне очень стыдно за этот код. Но по-другому не получилось
            var start = doc.Text.IndexOf(START_PATTERN, StringComparison.InvariantCultureIgnoreCase);
            start = doc.Text.IndexOf(">", start, StringComparison.InvariantCultureIgnoreCase) + 1;

            var stop = doc.Text.IndexOf(END_PATTERN, start, StringComparison.InvariantCultureIgnoreCase);
            for (var i = stop;; i--) {
                if (doc.Text[i] == '<') {
                    stop = i - 1;
                    break;
                }
            }

            doc.LoadHtml(doc.Text[start..stop]);
            
            var sr = new StringReader(doc.DocumentNode.InnerHtml.HtmlDecode());
            var text = new StringBuilder();
            while (true) {
                var line = await sr.ReadLineAsync();
                if (line == null) {
                    break;
                }
                
                if (!string.IsNullOrWhiteSpace(line)) {
                    var htmlDoc = line.AsHtmlDoc();
                    foreach (var node in htmlDoc.DocumentNode.ChildNodes) {
                        if (!string.IsNullOrWhiteSpace(node.InnerText) || node.QuerySelector("img") != null) {
                            text.AppendLine($"<p>{node.InnerHtml.Trim()}</p>");
                        }
                    }
                }
            }
            

            var chapterDoc = text.ToString().HtmlDecode().AsHtmlDoc();
            chapter.Images = await GetImages(chapterDoc, url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = title;
            
            return new List<Chapter>{ chapter };
        }
    }
}