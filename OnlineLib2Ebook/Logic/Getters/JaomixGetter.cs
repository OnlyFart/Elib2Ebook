using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using OnlineLib2Ebook.Configs;
using OnlineLib2Ebook.Extensions;
using OnlineLib2Ebook.Types.Book;
using OnlineLib2Ebook.Types.Jaomix;

namespace OnlineLib2Ebook.Logic.Getters {
    public class JaomixGetter : GetterBase {
        public JaomixGetter(BookGetterConfig config) : base(config) { }
        protected override Uri SystemUrl => new("https://jaomix.ru/");
        public override async Task<Book> Get(Uri url) {
            Init();
            url = await GetMainUrl(url);
            var bookId = GetId(url);
            var uri = new Uri($"https://jaomix.ru/category/{bookId}/");
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(uri);

            var book = new Book(bookId) {
                Cover = await GetCover(doc, uri),
                Chapters = await FillChapters(doc, uri),
                Title = HttpUtility.HtmlDecode(doc.GetByFilter("h1").InnerText.Trim()),
                Author = "Jaomix"
            };
            
            return book;
        }

        private async Task<Uri> GetMainUrl(Uri url) {
            if (url.Segments[1] != "category/") {
                var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
                var div = doc.DocumentNode.GetByFilterContains("span", "entry-category");
                return new Uri(url, div.Descendants().FirstOrDefault(t => t.Name == "a").Attributes["href"].Value);
            }

            return url;
        }

        private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
            var result = new List<Chapter>();

            foreach (var jaomixChapter in await GetChapters(doc, url)) {
                Console.WriteLine($"Загружаем главу \"{jaomixChapter.Title}\"");
                var chapter = new Chapter();
                var chapterDoc = HttpUtility.HtmlDecode(await GetChapter(jaomixChapter.Url)).AsHtmlDoc();
                chapter.Images = await GetImages(chapterDoc, url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
                chapter.Title = jaomixChapter.Title;

                result.Add(chapter);
            }

            return result;
        }

        private async Task<string> GetChapter(Uri jaomixChapterUrl) {
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(jaomixChapterUrl);
            var sb = new StringBuilder();
            
            foreach (var node in doc.DocumentNode.GetByFilterContains("div", "themeform").ChildNodes) {
                if (node.Name != "br" && node.Name != "script" && !string.IsNullOrWhiteSpace(node.InnerHtml) && node.Attributes["class"]?.Value?.Contains("adblock-service") == null) {
                    var tag = node.Name == "#text" ? "p" : node.Name;
                    sb.AppendLine($"<{tag}>{node.InnerHtml.Trim()}</{tag}>");
                }
            }
            
            return sb.ToString();
        }

        private async Task<IEnumerable<JaomixChapter>> GetChapters(HtmlDocument doc, Uri url) {
            var termId = doc.GetByFilter("div", "like-but").Id;

            var data = new Dictionary<string, string> {
                { "action", "toc" },
                { "selectall", termId }
            };
            
            var chapters = new List<JaomixChapter>();
            chapters.AddRange(ParseChapters(doc, url));
            
            var post = await _config.Client.PostAsync("https://jaomix.ru/wp-admin/admin-ajax.php", new FormUrlEncodedContent(data));
            var content = await post.Content.ReadAsStringAsync();
            doc = content.AsHtmlDoc();
            
            var toc = doc.DocumentNode.GetByFilterContains("select", "sel-toc");

            Console.WriteLine("Получаем оглавление");
            
            foreach (var option in toc.ChildNodes) {
                var pageId = option.Attributes["value"].Value;
                if (pageId == "0") {
                    continue;
                }
                
                data = new Dictionary<string, string> {
                    { "action", "toc" },
                    { "page", pageId },
                    { "termid", termId }
                };
                
                post = await _config.Client.PostAsync("https://jaomix.ru/wp-admin/admin-ajax.php", new FormUrlEncodedContent(data));
                content = await post.Content.ReadAsStringAsync();
                chapters.AddRange(ParseChapters(content.AsHtmlDoc(), url));
            }
            Console.WriteLine($"Получено {chapters.Count} глав");

            chapters.Reverse();
            return chapters;
        }
        
        private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
            var imagePath = doc.GetByFilter("div", "img-book")
                ?.Descendants()
                ?.FirstOrDefault(t => t.Name == "img")
                ?.Attributes["src"]?.Value;

            return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
        }

        private IEnumerable<JaomixChapter> ParseChapters(HtmlDocument doc, Uri url) {
            return doc.DocumentNode
                .GetByFilterContains("div", "hiddenstab")
                .Descendants()
                .Where(t => t.Name == "a")
                .Select(a => new JaomixChapter(a.InnerText.Trim(), new Uri(url, a.Attributes["href"].Value)));
        }
    }
}