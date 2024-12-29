using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Misc;
using Core.Types.Book;
using Core.Types.Common;
using Core.Types.StrokiMts;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters;

public class StrokiMtsGetter(BookGetterConfig config) : GetterBase(config) {
    protected override Uri SystemUrl => new("https://stroki.mts.ru/");

    protected override string GetId(Uri url) {
        return url.GetSegment(2).Split("-").Last();
    }

    public override Task Authorize() {
        var token = Config.Options.Login ?? Config.Options.Password;
        if (string.IsNullOrWhiteSpace(token)) {
            throw new Exception("Не указан авторизационный token.");
        }
        
        Config.Client.DefaultRequestHeaders.Add("access-token", token);
        return Task.CompletedTask;
    }
    
    public override async Task<Book> Get(Uri url) {
        var id = GetId(url);
        
        var details = await GetBook(id);
        var detailBook = details.Items.FirstOrDefault(d => d.TextBook != default)?.TextBook ??
                         details.Items.FirstOrDefault(d => d.AudioBook != default)?.AudioBook ??
                         details.Items.FirstOrDefault(d => d.ComicBook != default)?.ComicBook ??
                         details.Items.FirstOrDefault(d => d.PressBook != default)?.PressBook;

        if (detailBook == default) {
            throw new Exception("Не удалось получить книгу");
        }
        
        var book = new Book(url) {
            Cover = await GetCover(detailBook),
            Title = detailBook.Title,
            Author = GetAuthor(detailBook),
            CoAuthors = GetCoAuthors(detailBook),
            Annotation = detailBook.Annotation,
        };
        
        var origBook = await GetOriginalFile(id);
        if (origBook.Extension == ".epub") {
            book.Chapters = await FillChaptersFromEpub(origBook);
        } else {
            Config.Logger.LogInformation("Эта книга не в формате epub. Получить главы невозможно");
        }

        if (Config.Options.HasAdditionalType(AdditionalTypeEnum.Books)) {
            book.AdditionalFiles.Add(AdditionalTypeEnum.Books, origBook);
        }

        if (Config.Options.HasAdditionalType(AdditionalTypeEnum.Audio)) {
            book.AdditionalFiles.Add(AdditionalTypeEnum.Audio, await GetAudio(details));
        }
        
        return book;
    }

    private async Task<TempFile> GetOriginalFile(string bookId) {
        var fileMeta = await GetFileMeta(bookId);
        var fileUrl = await GetFileUrl(fileMeta.FirstOrDefault());
        var response = await Config.Client.GetWithTriesAsync(fileUrl.Url.AsUri());
        Config.Logger.LogInformation($"Оригинальный файл доступен по ссылке {fileUrl.Url.AsUri()}");
        return await TempFile.Create(fileUrl.Url.AsUri(), Config.TempFolder.Path, fileUrl.Url.AsUri().GetFileName(), await response.Content.ReadAsStreamAsync());
    }

    private async Task<List<TempFile>> GetAudio(StrokiMtsApiMultiResponse details) {
        var result = new List<TempFile>();

        var detail = details.Items.FirstOrDefault(d => d.AudioBook != default);
        if (detail == default) {
            return result;
        }

        var audio = detail.AudioBook;

        var fileMetas = await GetFileMeta(audio.Id.ToString());

        for (var i = 0; i < fileMetas.Length; i++) {
            var meta = fileMetas[i];
            var fileUrl = await GetFileUrl(meta);

            Config.Logger.LogInformation($"Загружаю аудиоверсию {i + 1}/{fileMetas.Length} {fileUrl.Url}");
            var response = await Config.Client.GetWithTriesAsync(fileUrl.Url.AsUri());
            result.Add(await TempFile.Create(fileUrl.Url.AsUri(), Config.TempFolder.Path, $"{i}_{fileUrl.Url.AsUri().GetFileName()}", await response.Content.ReadAsStreamAsync()));
            Config.Logger.LogInformation($"Аудиоверсия {i + 1}/{fileMetas.Length} {fileUrl.Url} загружена");
        }

        return result;
    }
    
    private Task<TempFile> GetCover(StrokiMtsBookItem book) {
        var url = book.ImageUrl.TryGetValue("extraLarge", out var item) ? item : book.ImageUrl.FirstOrDefault().Value;
        return !string.IsNullOrWhiteSpace(url) ? SaveImage(SystemUrl.MakeRelativeUri(url)) : Task.FromResult(default(TempFile));
    }
    
    private Author GetAuthor(StrokiMtsBookItem book) {
        var author = book.Authors.FirstOrDefault();
        return author == default ? new Author("Stroki") : new Author(author.Name, SystemUrl.MakeRelativeUri(author.FriendlyUrl));
    }
    
    private IEnumerable<Author> GetCoAuthors(StrokiMtsBookItem book) {
        return book.Authors.Skip(1).Select(author => new Author(author.Name, SystemUrl.MakeRelativeUri(author.FriendlyUrl))).ToList();
    }

    private async Task<StrokiMtsFile[]> GetFileMeta(string id) {
        var json = await SendAsync<StrokiMtsApiResponse<StrokiMtsFiles>>(() => GetMessage(SystemUrl.MakeRelativeUri("/api/books/files").AppendQueryParameter("bookId", id), "5.0", "5.0"));
        return json.Data.Full?.Length > 0 ? json.Data.Full : [json.Data.Preview];
    }
    
    private async Task<StrokiMtsFileUrl> GetFileUrl(StrokiMtsFile file) {
        var json = await SendAsync<StrokiMtsApiResponse<StrokiMtsFileUrl>>(() => GetMessage(SystemUrl.MakeRelativeUri($"api/books/files/data/link/{file.FileId}"), "5.0", "5.0"));
        return json.Data;
    }

    private async Task<StrokiMtsApiMultiResponse> GetBook(string id) {
        var json = await SendAsync<StrokiMtsApiResponse<StrokiMtsApiMultiResponse>>(() => GetMessage(SystemUrl.MakeRelativeUri($"/api/books/multi/{id}"), "5.38.0", "5.3"));
        return json.Data;
    }

    private async Task<T> SendAsync<T>(Func<HttpRequestMessage> message) {
        var response = await Config.Client.SendWithTriesAsync(message);
        if (response.StatusCode != HttpStatusCode.OK) {
            throw new Exception(await response.Content.ReadAsStringAsync());
        }
        
        return await response.Content.ReadFromJsonAsync<T>();
    }
    
    protected virtual HttpRequestMessage GetMessage(Uri uri, string appVersion, string apiVerions) {
        var message = new HttpRequestMessage(HttpMethod.Get, uri);

        foreach (var header in Config.Client.DefaultRequestHeaders) {
            message.Headers.Add(header.Key, header.Value);
        }
        
        message.Headers.Add("install-guid", Guid.NewGuid().ToString());
        message.Headers.Add("app-version", appVersion);
        message.Headers.Add("language", "ru");
        message.Headers.Add("platform", "ios");
        message.Headers.Add("api-version", apiVerions);
        
        message.Headers.Add("signature", GetSignature(uri));
        
        return message;
    }

    private static string GetSignature(Uri url) {
        var inputBytes = Encoding.UTF8.GetBytes(url.ToString().Replace("https://", "http://") + "meg@$h!t");
        var hashBytes = MD5.HashData(inputBytes);

        return Convert.ToHexString(hashBytes).ToLower();
    }
}