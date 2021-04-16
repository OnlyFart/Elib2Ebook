using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Author.Today.Epub.Converter.Extensions;
using Author.Today.Epub.Converter.Types.Book;
using Author.Today.Epub.Converter.Types.Response;
using HtmlAgilityPack;

namespace Author.Today.Epub.Converter.Logic {
    public class BookGetter : IDisposable {
        private readonly HttpClient _client;
        private readonly string _pattern;

        public BookGetter(HttpClient client, string pattern) {
            _client = client;
            _pattern = pattern;
        }
        
        /// <summary>
        /// Получение книги
        /// </summary>
        /// <param name="bookId">Идентификатор книги</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<BookMeta> Get(long bookId) {
            var bookUri = new Uri($"https://author.today/reader/{bookId}");
            var response = await _client.GetAsync(bookUri);

            if (response.StatusCode == HttpStatusCode.NotFound) {
                throw new Exception($"Книга {bookId} не существует.");
            }
            
            var content = await response.Content.ReadAsStringAsync();
            const string START_PATTERN = "chapters:";
            var startIndex = content.IndexOf(START_PATTERN, StringComparison.Ordinal) + START_PATTERN.Length;
            var endIndex = content.IndexOf("}],", startIndex, StringComparison.Ordinal) + 2;
            
            var metaContent = content[startIndex..endIndex].Trim().TrimEnd(';', ')');

            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var imagePath = doc.DocumentNode.Descendants()
                .FirstOrDefault(t => t.Name == "img" && t?.Attributes["class"]?.Value == "cover-image")
                ?.Attributes["src"]?.Value;

            var book = new BookMeta(bookId);
            if (!string.IsNullOrWhiteSpace(imagePath)) {
                book.Cover = await GetImage(new Uri(bookUri, imagePath));
            }

            book.Chapters = JsonSerializer.Deserialize<List<Chapter>>(metaContent);
            book.Title = doc.DocumentNode.Descendants().FirstOrDefault(t => t.Name == "div" && t.Attributes["class"]?.Value == "book-title")?.InnerText?.Trim();
            book.AuthorName = doc.DocumentNode.Descendants().FirstOrDefault(t => t.Name == "div" && t.Attributes["class"]?.Value == "book-author")?.InnerText?.Trim();
            
            await FillChapters(book);

            return book;
        }

        /// <summary>
        /// Дозагрузка различных паретров частей
        /// </summary>
        /// <param name="book"></param>
        private async Task FillChapters(BookMeta book) {
            foreach (var chapter in book.Chapters) {
                chapter.Path = new Uri($"https://author.today/reader/{book.Id}/chapter?id={chapter.Id}");
                var response = await _client.GetAsync(chapter.Path);
                
                Console.WriteLine($"Получаем главу {chapter.Path}");
                
                var secret = GetSecret(response);
                if (string.IsNullOrWhiteSpace(secret)) {
                    Console.WriteLine($"Невозможно расшифровать главу {chapter.Path}. Возможно платный доступ.");
                    continue;
                }
                
                Console.WriteLine($"Расшифровывем главу {chapter.Path}. Секрет {secret}");

                var doc = CreateDoc(chapter, await GetText(response), secret);
                await FillImages(doc, chapter);
                chapter.Content = doc.AsString();
            }
        }

        /// <summary>
        /// Загрузка изображений отдельной части
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="chapter"></param>
        private async Task FillImages(HtmlDocument doc, Chapter chapter) {
            var imgUrls = doc.DocumentNode
                .Descendants()
                .Where(t => t.Name == "img");

            foreach (var img in imgUrls) {
                var path = img.Attributes["src"]?.Value;
                if (string.IsNullOrWhiteSpace(path)) {
                    continue;
                }
                
                var uri = new Uri(chapter.Path, path);
                
                chapter.Images.Add(await GetImage(uri));
                img.Attributes["src"].Value = uri.GetFileName();
            }
        }

        /// <summary>
        /// Создание html документа
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="encodedText"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        private HtmlDocument CreateDoc(Chapter chapter, string encodedText, string secret) {
            var doc = new HtmlDocument {
                OptionCheckSyntax = true, 
                OptionFixNestedTags = true,
                OptionOutputAsXml = true
            };

            doc.LoadHtml(_pattern.Replace("{title}", chapter.Title).Replace("{body}", Decode(secret, encodedText)));
            return doc;
        }

        /// <summary>
        /// Получение секрета для расшифровки контента книги
        /// </summary>
        /// <param name="response">Ответ сервера</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static string GetSecret(HttpResponseMessage response) {
            if (response.Headers.Contains("Reader-Secret")) {
                foreach (var header in response.Headers.GetValues("Reader-Secret")) {
                    return string.Join("", header.Reverse()) + "@_@";
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Получение зашифрованного текста главы книги
        /// </summary>
        /// <param name="response">Ответ сервера</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static async Task<string> GetText(HttpResponseMessage response) {
            var data = await response.Content.ReadFromJsonAsync<Response>();
            if (string.IsNullOrWhiteSpace(data?.Data?.Text)) {
                throw new Exception("Не удалось десериализовать ответ, возможно поменялся формат, расшифровка книги невозможна");
            }

            return data.Data.Text;
        }

        /// <summary>
        /// Расшифровка контента главы книги с использованием ключа
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="encodedText"></param>
        /// <returns></returns>
        private static string Decode(string secret, string encodedText) {
            var sb = new StringBuilder();
            for (var i = 0; i < encodedText.Length; i++) {
                sb.Append((char) (encodedText[i] ^ secret[i % secret.Length]));
            }

            return HttpUtility.HtmlDecode(sb.ToString());
        }

        /// <summary>
        /// Получение изображения
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private async Task<Image> GetImage(Uri uri) {
            return new() {
                Path = uri.GetFileName(), 
                Content = await _client.GetByteArrayAsync(uri)
            };
        }

        public void Dispose() {
            _client?.Dispose();
        }
    }
}
