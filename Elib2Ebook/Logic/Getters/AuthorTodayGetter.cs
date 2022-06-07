using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.AuthorToday;
using HtmlAgilityPack;

namespace Elib2Ebook.Logic.Getters; 

public class AuthorTodayGetter : GetterBase {
    public AuthorTodayGetter(BookGetterConfig config) : base(config) { }

    protected override Uri SystemUrl => new("https://author.today/");

    private string UserId { get; set; } = string.Empty;
    
    protected override string GetId(Uri url) {
        return url.Segments[2].Trim('/');
    }

    /// <summary>
    /// Получение книги
    /// </summary>
    /// <param name="url">Ссылка на книгу</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public override async Task<Book> Get(Uri url) { 
        var details = await GetBookDetails(GetId(url));

        var book = new Book(url) {
            Cover = await GetCover(details),
            Chapters = await FillChapters(details),
            Title = details.Title,
            Author = GetAuthor(details),
            Annotation = details.Annotation,
            Seria = GetSeria(details)
        };
        
        return book;
    }

    private async Task<AuthorTodayBookDetails> GetBookDetails(string bookId) {
        var response = await _config.Client.GetWithTriesAsync(new Uri($"https://api.author.today/v1/work/{bookId}/details"));
        if (response.StatusCode != HttpStatusCode.OK) {
            throw new Exception("Книга не найдена");
        }

        return await response.Content.ReadFromJsonAsync<AuthorTodayBookDetails>();
    }

    private static Author GetAuthor(AuthorTodayBookDetails book) {
        return new Author(book.AuthorFio, new Uri($"https://author.today/u/{book.AuthorUserName}/works"));
    }

    private static Seria GetSeria(AuthorTodayBookDetails book) {
        if (!book.SeriesId.HasValue) {
            return default;
        }

        return new Seria {
            Name = book.SeriesTitle,
            Number = book.SeriesWorkNumber.HasValue ? book.SeriesWorkNumber.ToString() : string.Empty,
            Url = new Uri($"https://author.today/work/series/{book.SeriesId}")
        };
    }

    /// <summary>
    /// Авторизация в системе
    /// </summary>
    /// <exception cref="Exception"></exception>
    public override async Task Authorize() {
        _config.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "guest");
        if (!_config.HasCredentials) {
            return;
        }

        var response = await _config.Client.PostAsJsonAsync(new Uri("https://api.author.today/v1/account/login-by-password"), new { _config.Login, _config.Password });
        var data = await response.Content.ReadFromJsonAsync<AuthorTodayAuthResponse>();
        if (!string.IsNullOrWhiteSpace(data?.Token)) {
            Console.WriteLine("Успешно авторизовались");
            _config.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.Token);

            var user = await _config.Client.GetFromJsonAsync<AuthorTodayUser>(new Uri("https://api.author.today/v1/account/current-user"));
            UserId = user!.Id.ToString();
        } else {
            throw new Exception($"Не удалось авторизоваться. {data?.Message}"); 
        }
    }
    
    private Task<Image> GetCover(AuthorTodayBookDetails book) {
        return !string.IsNullOrWhiteSpace(book.CoverUrl) ? GetImage(new Uri(book.CoverUrl)) : Task.FromResult(default(Image));
    }
    
    private async Task<IEnumerable<Chapter>> FillChapters(AuthorTodayBookDetails book) {
        var chapters = new List<Chapter>();
        foreach (var atChapter in await GetChapters(book)) {
            var atChapterTitle = atChapter.Title.ReplaceNewLine();
            Console.WriteLine($"Загружаю главу {atChapterTitle.CoverQuotes()}");
            var chapter = new Chapter();
            var chapterDoc = Decode(atChapter);

            chapter.Title = atChapterTitle;
            chapter.Images = await GetImages(chapterDoc, new Uri("https://author.today/"));
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            
            chapters.Add(chapter);
        }
            
        return chapters;
    }

    private async Task<IEnumerable<AuthorTodayChapter>> GetChapters(AuthorTodayBookDetails book) {
        var result = new List<AuthorTodayChapter>();
        
        foreach (var chunk in book.Chapters.OrderBy(c => c.SortOrder).Chunk(100)) {
            var ids = string.Join("&", chunk.Select((c, i) => $"ids[{i}]={c.Id}"));
            var uri = new Uri($"https://api.author.today/v1/work/{book.Id}/chapter/many-texts?{ids}");
            var chapters = await _config.Client.GetFromJsonAsync<AuthorTodayChapter[]>(uri);
            if (chapters != default) {
                result.AddRange(chapters.Where(c => c.IsSuccessful));
            }
        }

        return result;
    }

    private HtmlDocument Decode(AuthorTodayChapter chapter) {
        var secret = string.Join("", chapter.Key.Reverse()) + "@_@" + UserId;
        var sb = new StringBuilder();
        for (var i = 0; i < chapter.Text.Length; i++) {
            sb.Append((char) (chapter.Text[i] ^ secret[i % secret.Length]));
        }

        return sb.ToString().AsHtmlDoc();
    }
}