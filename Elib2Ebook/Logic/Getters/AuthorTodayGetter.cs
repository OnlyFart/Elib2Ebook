using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.AuthorToday;
using Elib2Ebook.Types.Book;

namespace Elib2Ebook.Logic.Getters; 

public class AuthorTodayGetter : GetterBase {
    public AuthorTodayGetter(BookGetterConfig config) : base(config) { }

    protected override Uri SystemUrl => new("https://author.today/");

    private string UserId { get; set; } = string.Empty;
    
    protected override string GetId(Uri url) {
        return url.Segments[2].Trim('/');
    }
    
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
        var response = await Config.Client.GetWithTriesAsync(new Uri($"https://api.author.today/v1/work/{bookId}/details"));
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

    public override async Task Init() {
        await base.Init();
        
        Config.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "guest");
        Config.Client.Timeout = TimeSpan.FromSeconds(10);
    }

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }

        var response = await Config.Client.PostAsJsonAsync(new Uri("https://api.author.today/v1/account/login-by-password"), new { Config.Options.Login, Config.Options.Password });
        var data = await response.Content.ReadFromJsonAsync<AuthorTodayAuthResponse>();
        
        if (!string.IsNullOrWhiteSpace(data?.Token)) {
            Console.WriteLine("Успешно авторизовались");
            Config.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.Token);

            var user = await Config.Client.GetFromJsonAsync<AuthorTodayUser>(new Uri("https://api.author.today/v1/account/current-user"));
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
            var title = atChapter.Title.ReplaceNewLine();
            Console.WriteLine($"Загружаю главу {title.CoverQuotes()}");
            
            var chapter = new Chapter {
                Title = title
            };

            if (atChapter.IsSuccessful) {
                var chapterDoc = atChapter.Decode(UserId).AsHtmlDoc();
                chapter.Images = await GetImages(chapterDoc, new Uri("https://author.today/"));
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }

            chapters.Add(chapter);
        }
            
        return chapters;
    }

    private async Task<IEnumerable<AuthorTodayChapter>> GetChapters(AuthorTodayBookDetails book) {
        var result = new List<AuthorTodayChapter>();
        
        foreach (var chunk in book.Chapters.OrderBy(c => c.SortOrder).Chunk(100)) {
            var ids = string.Join("&", chunk.Select((c, i) => $"ids[{i}]={c.Id}"));
            var uri = new Uri($"https://api.author.today/v1/work/{book.Id}/chapter/many-texts?{ids}");
            var response = await Config.Client.GetWithTriesAsync(uri);
            var chapters = await response.Content.ReadFromJsonAsync<AuthorTodayChapter[]>();
            if (chapters != default) {
                result.AddRange(chapters);
            }
        }

        return result;
    }
}