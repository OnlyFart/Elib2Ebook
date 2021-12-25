using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using OnlineLib2Ebook.Configs;
using OnlineLib2Ebook.Extensions;
using OnlineLib2Ebook.Types.Book;
using OnlineLib2Ebook.Types.DakrNovels;

namespace OnlineLib2Ebook.Logic.Getters {
    public class DarkNovelsGetter : GetterBase {
        private static readonly Dictionary<int, char> _map = new();
        private static readonly string _alphabet = "аАбБвВгГдДеЕёЁжЖзЗиИйЙкКлЛмМнНоОпПрРсСтТуУфФхХцЦчЧшШщЩъЪыЫьЬэЭюЮяЯ";

        public DarkNovelsGetter(BookGetterConfig config) : base(config) {
            InitMap();
        }
        
        protected override Uri SystemUrl => new("https://dark-novels.ru/");

        private static void InitMap() {
            var start = 13338;
            const int shift = 38;
            foreach (var c in _alphabet) {
                for (var i = start; i < start + shift; i++) {
                    _map[i] = c;
                }

                start += shift;
            }
        }

        public override async Task<Book> Get(Uri url) {
            Init();
            url = await GetMainUrl(url);
            
            var bookFullId = GetId(url);
            var bookId = bookFullId.Split(".").Last();
            
            var uri = new Uri($"https://dark-novels.ru/{bookFullId}/");
            var doc = await _config.Client.GetHtmlDocWithTriesAsync(uri);

            var book = new Book {
                Cover = await GetCover(doc, uri),
                Chapters = await FillChapters(bookId),
                Title = doc.GetTextBySelector("h2.display-1"),
                Author = "DarkNovels"
            };
            
            return book;
        }
        
        private async Task<Uri> GetMainUrl(Uri url) {
            if (url.Segments[1] == "read/") {
                var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);
                var id = Regex.Match(doc.DocumentNode.InnerHtml, "slug:\"(?<id>.*?)\"");
                return new Uri($"https://dark-novels.ru/{id.Groups["id"].Value}");
            }

            return url;
        }
        
        private async Task<IEnumerable<Chapter>> FillChapters(string bookId) {
            var result = new List<Chapter>();

            foreach (var darkNovelsChapter in await GetChapters(bookId)) {
                Console.WriteLine($"Загружаем главу \"{darkNovelsChapter.Title}\"");
                if (darkNovelsChapter.Title.StartsWith("Volume:") || darkNovelsChapter.Payed == 1) {
                    continue;
                }
                
                var chapter = new Chapter();
                var chapterDoc = await GetChapter(bookId, darkNovelsChapter.Id);
                chapter.Images = await GetImages(chapterDoc, SystemUrl);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
                chapter.Title = darkNovelsChapter.Title;

                result.Add(chapter);
            }

            return result;
        }
        
        private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
            var imagePath = doc.QuerySelector("div.book-cover-container img")?.Attributes["data-src"]?.Value;
            return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
        }

        private async Task<DarkNovelsChapter[]> GetChapters(string bookId) {
            return await _config.Client.GetFromJsonAsync<DarkNovelsData<DarkNovelsChapter[]>>($"https://api.dark-novels.ru/v2/toc/{bookId}").ContinueWith(t => t.Result.Data);
        }

        private async Task<HtmlDocument> GetChapter(string bookId, int chapterId) {
            var data = await _config.Client.PostWithTriesAsync(new Uri("https://api.dark-novels.ru/v2/chapter/"), GetData(bookId, chapterId));
            if (data.StatusCode == HttpStatusCode.BadRequest) {
                return new HtmlDocument();
            }

            using var zip = new ZipArchive(await data.Content.ReadAsStreamAsync());
            var sb = new StringBuilder();
            foreach (var entry in zip.Entries) {
                using var sr = new StreamReader(entry.Open());
                foreach (var c in await sr.ReadToEndAsync()) {
                    sb.Append(_map.TryGetValue(c, out var d) ? d : c);
                }
            }

            return sb.ToString().HtmlDecode().AsHtmlDoc().RemoveNodes(d => d.Name == "h1");
        }

        private static MultipartFormDataContent GetData(string bookId, int chapterId) {
            return new MultipartFormDataContent {
                {new StringContent(bookId), "b"},
                {new StringContent("html"), "f"},
                {new StringContent(chapterId.ToString()), "c"}
            };
        }
    }
}