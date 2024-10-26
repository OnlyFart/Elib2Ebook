using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.BookYandex;
using Core.Types.Common;

namespace Core.Logic.Getters.BooksYandex;

public abstract class BooksYandexGetterBase : GetterBase {
    protected override Uri SystemUrl => new("https://books.yandex.ru/");
    
    protected abstract string[] Paths { get; }
    
    protected override string GetId(Uri url) {
        return url.GetSegment(2);
    }

    public override bool IsSameUrl(Uri url) {
        return base.IsSameUrl(url) && Paths.Any(path => string.Equals(url.GetSegment(1), path, StringComparison.InvariantCultureIgnoreCase));
    }

    protected BooksYandexGetterBase(BookGetterConfig config) : base(config) {
        
    }
    
    public override Task Authorize() {
        var token = Config.Options.Login ?? Config.Options.Password;
        if (string.IsNullOrWhiteSpace(token)) {
            throw new Exception("Не указан авторизационный token.");
        }
        
        Config.Client.DefaultRequestHeaders.Add("auth-token", token);
        return Task.CompletedTask;
    }
    
    public override async Task<Book> Get(Uri url) {
        var id = GetId(url);

        var path = url.GetSegment(1);
        url = SystemUrl.MakeRelativeUri($"/{path}/{id}");
        
        var bookResponse = await GetBookResponse(path, id);
        var details = (BookmateBookBase)bookResponse.Book ?? (BookmateBookBase)bookResponse.AudioBook ?? bookResponse.Comicbook;
        
        var book = new Book(url) {
            Cover = await GetCover(details),
            Title = details.Title,
            Author = GetAuthor(details),
            Annotation = details.Annotation,
            Lang = details.Language,
        };

        book.Chapters = await FillChapters(book, bookResponse);
        
        return book;
    }

    protected virtual Task<IEnumerable<Chapter>> FillChapters(Book book, BooksYandexResponse response) {
        return Task.FromResult<IEnumerable<Chapter>>([]);
    }
    
    private async Task<BooksYandexResponse> GetBookResponse(string path, string id) {
        try {
            var response = await Config.Client.GetAsync($"https://api.bookmate.ru/api/v5/{path}/{id}".AsUri());
            if (response.StatusCode != HttpStatusCode.OK) {
                response = await Config.Client.GetAsync($"https://api.bookmate.ru/api/v5/books/{id}".AsUri());
            }

            var booksYandexResponse = await response.Content.ReadFromJsonAsync<BooksYandexResponse>();
            
            if (booksYandexResponse.AudioBook?.LinkedBooks?.Length > 0) {
                var linkedResponse = await Config.Client.GetAsync($"https://api.bookmate.ru/api/v5/books/{booksYandexResponse.AudioBook.LinkedBooks[0]}".AsUri());
                if (linkedResponse.StatusCode == HttpStatusCode.OK) {
                    return await linkedResponse.Content.ReadFromJsonAsync<BooksYandexResponse>();
                }
            }

            return booksYandexResponse;
        } catch (HttpRequestException ex) {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) {
                throw new Exception("Авторизационный токен невалиден. Требуется обновление");
            }

            throw;
        }
    }
    
    private Task<TempFile> GetCover(BookmateBookBase book) {
        var url = book.Cover.Large ?? book.Cover.Small;
        return !string.IsNullOrWhiteSpace(url) ? SaveImage(SystemUrl.MakeRelativeUri(url)) : Task.FromResult(default(TempFile));
    }
    
    private Author GetAuthor(BookmateBookBase book) {
        var author = book.GetAuthor();
        return author == default ? new Author("BooksYandex") : new Author(author.Name, SystemUrl.MakeRelativeUri($"/authors/{author.Uuid}"));
    }
}