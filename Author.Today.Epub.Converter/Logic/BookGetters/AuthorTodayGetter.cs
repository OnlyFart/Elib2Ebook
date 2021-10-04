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
using Author.Today.Epub.Converter.Exceptions;
using Author.Today.Epub.Converter.Extensions;
using Author.Today.Epub.Converter.Types.AuthorToday.Response;
using Author.Today.Epub.Converter.Types.Book;
using HtmlAgilityPack;

namespace Author.Today.Epub.Converter.Logic.BookGetters {
    public class AuthorTodayGetter : GetterBase {
        private readonly Regex _userIdRgx = new("userId: (?<userId>\\d+),");

        public AuthorTodayGetter(BookGetterConfig config) : base(config) {

        }

        public override Uri SystemUrl => new("https://author.today");

        /// <summary>
        /// Получение книги
        /// </summary>
        /// <param name="url">Ссылка на книгу</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public override async Task<Book> Get(Uri url) {
            var bookId = GetId(url);
            await Authorize();
            
            var bookUri = new Uri($"https://author.today/reader/{bookId}");
            Console.WriteLine($"Загружаем книгу {bookUri.ToString().CoverQuotes()}");
            using var response = await _config.Client.GetStringWithTriesAsync(bookUri);

            switch (response.StatusCode) {
                case HttpStatusCode.NotFound:
                    throw new BookNotFoundException(bookId);
                case HttpStatusCode.Forbidden:
                    throw new BookForbiddenException(bookId);
            }

            var content = await response.Content.ReadAsStringAsync();
            var doc = content.AsHtmlDoc();
            
            return new Book(bookId) {
                Cover = await GetCover(doc, bookUri),
                Chapters = await FillChapters(content, long.Parse(bookId), GetUserId(content)),
                Title = HttpUtility.HtmlDecode(doc.GetTextByFilter("div", "book-title")),
                Author = HttpUtility.HtmlDecode(doc.GetTextByFilter("div", "book-author"))
            };
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
        
        public MultipartFormDataContent GenerateAuthData(string token) {
            return new() {
                {new StringContent(token), "__RequestVerificationToken"},
                {new StringContent(_config.Login), "Login"},
                {new StringContent(_config.Password), "Password"}
            };
        }

        /// <summary>
        /// Авторизация в системе
        /// </summary>
        /// <exception cref="Exception"></exception>
        private async Task Authorize(){
            if (!_config.HasCredentials) {
                return;
            }
            
            var doc = await _config.Client
                .GetStringAsync("https://author.today/")
                .ContinueWith(t => t.Result.AsHtmlDoc());

            var token = doc.GetAttributeByNameAttribute("__RequestVerificationToken", "value");

            using var post = await _config.Client.PostAsync("https://author.today/account/login", GenerateAuthData(token));
            var response = await post.Content.ReadFromJsonAsync<ApiResponse<object>>();
            
            if (response?.IsSuccessful == true) {
                Console.WriteLine("Успешно авторизовались");
            } else {
                throw new Exception($"Не удалось авторизоваться. {response?.Messages?.FirstOrDefault()}");
            }
        }

        /// <summary>
        /// Получение обложки
        /// </summary>
        /// <param name="doc">HtmlDocument</param>
        /// <param name="bookUri">Адрес страницы с книгой</param>
        /// <returns></returns>
        private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
            var imagePath = doc.DocumentNode.Descendants()
                .FirstOrDefault(t => t.Name == "img" && t.Attributes["class"]?.Value == "cover-image")
                ?.Attributes["src"]?.Value;

            return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
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
        /// <param name="content">Контент книги</param>
        /// <param name="bookId">Идентификатор книги</param>
        /// <param name="userId">Идентификатор пользователя</param>
        private async Task<IEnumerable<Chapter>> FillChapters(string content, long bookId, string userId) {
            var chapters = GetChapters(content);
            
            foreach (var chapter in chapters) {
                var chapterUri = new Uri($"https://author.today/reader/{bookId}/chapter?id={chapter.Id}");
                
                Console.WriteLine($"Получаем главу {chapter.Title.CoverQuotes()}");
                using var response = await _config.Client.GetStringWithTriesAsync(chapterUri);

                if (response is not { StatusCode: HttpStatusCode.OK }) {
                    throw new Exception($"Не удалось получить главу {chapter.Title.CoverQuotes()}");
                }

                var secret = GetSecret(response, userId);
                if (string.IsNullOrWhiteSpace(secret)) {
                    Console.WriteLine($"Невозможно расшифровать главу {chapter.Title.CoverQuotes()}. Возможно, платный доступ.");
                    continue;
                }
                
                Console.WriteLine($"Расшифровываем главу {chapter.Title.CoverQuotes()}. Секрет {secret.CoverQuotes()}");
                var decodeText = Decode(await GetText(response), secret);
                
                // Порядок вызова функций важен. В методе GetImages происходит
                // исправления урлов картинок для их отображения в epub документе
                var doc = decodeText.AsHtmlDoc();
                chapter.Images = await GetImages(doc, chapterUri);
                chapter.Content = doc.DocumentNode.InnerHtml;
            }
            
            return chapters;
        }

        /// <summary>
        /// Получение секрета для расшифровки контента книги
        /// </summary>
        /// <param name="response">Ответ сервера</param>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static string GetSecret(HttpResponseMessage response, string userId) {
            if (!response.Headers.Contains("Reader-Secret")) {
                return string.Empty;
            }
            
            foreach (var header in response.Headers.GetValues("Reader-Secret")) {
                return string.Join("", header.Reverse()) + "@_@" + userId;
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
            if (data?.IsSuccessful == false) {
                throw new Exception($"Не удалось получить контент части. {data.Messages.FirstOrDefault()}");
            }

            return data?.Data.Text ?? string.Empty;
        }

        /// <summary>
        /// Расшифровка контента главы книги с использованием ключа
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="encodedText"></param>
        /// <returns></returns>
        private static string Decode(string encodedText, string secret) {
            var sb = new StringBuilder();
            for (var i = 0; i < encodedText.Length; i++) {
                sb.Append((char) (encodedText[i] ^ secret[i % secret.Length]));
            }

            return HttpUtility.HtmlDecode(sb.ToString());
        }
    }
}
