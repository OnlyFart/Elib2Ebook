using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Author.Today.Epub.Converter.Configs;
using Author.Today.Epub.Converter.Extensions;
using Author.Today.Epub.Converter.Types.Book;
using Author.Today.Epub.Converter.Types.Response;
using HtmlAgilityPack;

namespace Author.Today.Epub.Converter.Logic {
    public class BookGetter : IDisposable {
        private readonly Regex _userIdRgx = new("userId: (?<userId>\\d+),");
        
        private readonly BookGetterConfig _config;

        public BookGetter(BookGetterConfig config) {
            _config = config;
        }

        /// <summary>
        /// Получение книги
        /// </summary>
        /// <param name="bookId">Идентификатор книги</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<BookMeta> Get(long bookId){
            await Authorize();
            
            var bookUri = new Uri($"https://author.today/reader/{bookId}");
            var response = await _config.Client.GetAsync(bookUri);

            if (response.StatusCode == HttpStatusCode.NotFound) {
                throw new Exception($"Книга {bookId} не существует.");
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var doc = content.AsHtmlDoc();
            
            var book = new BookMeta(bookId) {
                Cover = await GetCover(doc, bookUri),
                Chapters = GetChapters(content),
                Title = doc.GetTextByFilter("div", "book-title"),
                Author = doc.GetTextByFilter("div", "book-author")
            };

            return await FillChapters(book, GetUserId(content));
        }

        /// <summary>
        /// Получение идентификатора пользователя из контента
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private string GetUserId(string content) {
            var match = _userIdRgx.Match(content);
            return match.Success ? match.Groups["userId"].Value : string.Empty;
        }

        /// <summary>
        /// Авторизация в системе
        /// </summary>
        /// <exception cref="Exception"></exception>
        private async Task Authorize() {
            if (!string.IsNullOrWhiteSpace(_config.Login) && !string.IsNullOrWhiteSpace(_config.Password)) {
                var mainPage = await _config.Client.GetStringAsync("https://author.today/");
                var doc = mainPage.AsHtmlDoc();

                var token = doc.GetAttributeByNameAttribute("__RequestVerificationToken", "value");

                var form = new MultipartFormDataContent {
                    {new StringContent(token), "__RequestVerificationToken"},
                    {new StringContent(_config.Login), "Login"},
                    {new StringContent(_config.Password), "Password"}
                };

                var post = await _config.Client.PostAsync("https://author.today/account/login", form);
                var response = await post.Content.ReadFromJsonAsync<ApiResponse<LoginData>>();
                if (response?.IsSuccessful == true) {
                    Console.WriteLine("Успешно авторизовались");
                } else {
                    throw new Exception($"Не удалось авторизоваться. {response?.Messages?.FirstOrDefault()}");
                }
            }
        }

        /// <summary>
        /// Получение обложки
        /// </summary>
        /// <param name="doc">HtmlDocument</param>
        /// <param name="bookUri">Адрес страницы с книгой</param>
        /// <returns></returns>
        private async Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
            var imagePath = doc.DocumentNode.Descendants()
                .FirstOrDefault(t => t.Name == "img" && t?.Attributes["class"]?.Value == "cover-image")
                ?.Attributes["src"]?.Value;

            return !string.IsNullOrWhiteSpace(imagePath) ? await GetImage(new Uri(bookUri, imagePath)) : null;
        }

        /// <summary>
        /// Получение списка частей из кода страницы
        /// </summary>
        /// <param name="content">Код страницы</param>
        /// <returns></returns>
        private static List<Chapter> GetChapters(string content) {
            const string START_PATTERN = "chapters:";
            var startIndex = content.IndexOf(START_PATTERN, StringComparison.Ordinal) + START_PATTERN.Length;
            var endIndex = content.IndexOf("}],", startIndex, StringComparison.Ordinal) + 2;
            
            var metaContent = content[startIndex..endIndex].Trim().TrimEnd(';', ')');
            return JsonSerializer.Deserialize<List<Chapter>>(metaContent);
        }

        /// <summary>
        /// Дозагрузка различных пареметров частей
        /// </summary>
        /// <param name="book">Книга</param>
        /// <param name="userId">Идентификатор пользователя</param>
        private async Task<BookMeta> FillChapters(BookMeta book, string userId) {
            foreach (var chapter in book.Chapters) {
                chapter.Path = new Uri($"https://author.today/reader/{book.Id}/chapter?id={chapter.Id}");
                var response = await _config.Client.GetAsync(chapter.Path);
                
                Console.WriteLine($"Получаем главу {chapter.Path}");
                
                var secret = GetSecret(response, userId);
                if (string.IsNullOrWhiteSpace(secret)) {
                    Console.WriteLine($"Невозможно расшифровать главу {chapter.Path}. Возможно платный доступ.");
                    continue;
                }
                
                Console.WriteLine($"Расшифровываем главу {chapter.Path}. Секрет {secret}");

                var doc = GenerateXhtml(chapter, await GetText(response), secret).AsXHtmlDoc();
                await FillImages(doc, chapter);
                chapter.Content = doc.AsString();
            }
            
            return book;
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
        /// Создание Xhtml документа из кода части
        /// </summary>
        /// <param name="chapter">Часть</param>
        /// <param name="encodedText">Закодированный текст</param>
        /// <param name="secret">Секрет для расшифровки</param>
        /// <returns></returns>
        private string GenerateXhtml(Chapter chapter, string encodedText, string secret) {
            return _config.Pattern.Replace("{title}", chapter.Title).Replace("{body}", Decode(secret, encodedText));
        }

        /// <summary>
        /// Получение секрета для расшифровки контента книги
        /// </summary>
        /// <param name="response">Ответ сервера</param>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static string GetSecret(HttpResponseMessage response, string userId) {
            if (response.Headers.Contains("Reader-Secret")) {
                foreach (var header in response.Headers.GetValues("Reader-Secret")) {
                    return string.Join("", header.Reverse()) + "@_@" + userId;
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
            var data = await response.Content.ReadFromJsonAsync<ApiResponse<ChapterData>>();
            if (string.IsNullOrWhiteSpace(data?.Data?.Text)) {
                throw new Exception("Не удалось десериализовать ответ, возможно поменялся формат, расшифровка книги невозможна.");
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
            return new(uri.GetFileName(), await _config.Client.GetByteArrayAsync(uri));
        }

        public void Dispose() {
            _config.Client?.Dispose();
        }
    }
}
