using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Misc;
using Core.Types.Book;
using Core.Types.Bookmate;
using Core.Types.Common;
using EpubSharp;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters;

public class BookmateGetter : GetterBase {
    public BookmateGetter(BookGetterConfig config) : base(config) { }

    protected override Uri SystemUrl => new("https://bookmate.ru/");

    protected override string GetId(Uri url) {
        return url.GetSegment(2);
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
            Lang = details.Language
        };
        
        if (path == "comicbooks") {
            book.Chapters = await FillChaptersComic(bookResponse);
        } else {
            var originalBook = await GetEpubFile(bookResponse);
            book.Chapters = await FillChapters(originalBook);
            
            if (Config.Options.HasAdditionalType(AdditionalTypeEnum.Books)) {
                book.AdditionalFiles.Add(AdditionalTypeEnum.Books, originalBook);
            }
        }

        if (Config.Options.HasAdditionalType(AdditionalTypeEnum.Audio)) {
            book.AdditionalFiles.Add(AdditionalTypeEnum.Audio, await GetAudio(bookResponse));
        }
        
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChaptersComic(BookmateBookResponse bookResponse) {
        if (Config.Options.NoChapters || string.IsNullOrWhiteSpace(bookResponse.Comicbook?.UUID)) {
            return [];
        }
        
        var metadata = await Config.Client.GetFromJsonAsync<BookmateComicMetadata>($"https://api.bookmate.ru/api/v5/comicbooks/{bookResponse.Comicbook.UUID}/metadata.json");
        
        var sb = new StringBuilder();
        
        foreach (var page in metadata.Pages) {
            sb.Append($"<img src=\"{page.Content.Uri.Image}\" />");
        }

        var doc = sb.AsHtmlDoc();
        var chapter = new Chapter {
            Images = await GetImages(doc, SystemUrl),
            Content = doc.DocumentNode.InnerHtml,
            Title = bookResponse.Comicbook.Title
        };

        return [chapter];
    }

    private async Task<IEnumerable<Chapter>> FillChapters(TempFile file) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        await using var stream = file.GetStream();
        var epubBook = EpubReader.Read(stream, true, Encoding.UTF8);
        var current = epubBook.TableOfContents.First();

        do {
            Config.Logger.LogInformation($"Загружаю главу {current.Title.CoverQuotes()}");

            var chapter = new Chapter {
                Title = current.Title
            };

            var content = GetContent(epubBook, current);
            chapter.Images = await GetImages(content, epubBook);
            chapter.Content = content.DocumentNode.RemoveNodes("h1, h2, h3").InnerHtml;
            result.Add(chapter);
        } while ((current = current.Next) != default);

        return result;
    }

    private async Task<IEnumerable<TempFile>> GetImages(HtmlDocument doc, EpubBook book) {
        var images = new List<TempFile>();
        foreach (var img in doc.QuerySelectorAll("img")) {
            var path = img.Attributes["src"]?.Value;
            if (string.IsNullOrWhiteSpace(path)) {
                img.Remove();
                continue;
            }

            var t = book.Resources.Images.FirstOrDefault(i => i.AbsolutePath.EndsWith(path.Trim('.')));
            if (t == default) {
                img.Remove();
                continue;
            }

            if (t.Content == null || t.Content.Length == 0) {
                img.Remove();
                continue;
            }
            
            var image = await TempFile.Create(null, Config.TempFolder.Path, t.Href, t.Content);
            img.Attributes["src"].Value = image.FullName;
            images.Add(image);
        }

        return images;
    }
    
    private Task<TempFile> GetCover(BookmateBookBase book) {
        var url = book.Cover.Large ?? book.Cover.Small;
        return !string.IsNullOrWhiteSpace(url) ? SaveImage(SystemUrl.MakeRelativeUri(url)) : Task.FromResult(default(TempFile));
    }
    
    private Author GetAuthor(BookmateBookBase book) {
        var author = book.GetAuthor();
        return author == default ? new Author("Bookmate") : new Author(author.Name, SystemUrl.MakeRelativeUri($"/authors/{author.Uuid}"));
    }
    

    private async Task<TempFile> GetEpubFile(BookmateBookResponse bookResponse) {
        var id = bookResponse.Book?.UUID ?? bookResponse.AudioBook?.LinkedBooks?.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(id)) {
            return default;
        }
        
        var requestUri = $"https://api.bookmate.ru/api/v5/books/{id}/content/v4".AsUri();
        var epubResponse = await Config.Client.GetAsync(requestUri);
        return await TempFile.Create(requestUri, Config.TempFolder.Path, epubResponse.Content.Headers.ContentDisposition.FileName.Trim('\"'), await epubResponse.Content.ReadAsStreamAsync());
    }
    
    private async Task<List<TempFile>> GetAudio(BookmateBookResponse bookResponse) {
        var result = new List<TempFile>();
        var id = bookResponse.AudioBook?.UUID ?? bookResponse.Book?.LinkedAudio?.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(id)) {
            return result;
        }
        
        var playlist = await GetPlayList(id);
        
        if (playlist.Tracks.Length == 0) {
            return result;
        }

        for (var i = 0; i < playlist.Tracks.Length; i++) {
            var track = playlist.Tracks[i];
            var url = track.Offline.Max.Url.Replace(".m3u8", ".m4a");

            Config.Logger.LogInformation($"Загружаю аудиоверсию {i + 1}/{playlist.Tracks.Length} {url}");
            var response = await Config.Client.GetWithTriesAsync(url.AsUri());
            result.Add(await TempFile.Create(url.AsUri(), Config.TempFolder.Path, $"{i}_{url.AsUri().GetFileName()}", await response.Content.ReadAsStreamAsync()));
            Config.Logger.LogInformation($"Аудиоверсия {i + 1}/{playlist.Tracks.Length} {url} загружена");
        }

        return result;
    }

    private async Task<BookmatePlaylist> GetPlayList(string id) {
        try {
            var response = await Config.Client.GetFromJsonAsync<BookmatePlaylist>($"https://api.bookmate.ru/api/v5/audiobooks/{id}/playlists.json".AsUri());
            return response;
        } catch (HttpRequestException ex) {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) {
                throw new Exception("Авторизационный токен невалиден. Требуется обновление");
            }

            throw;
        }
    }
    
    private async Task<BookmateBookResponse> GetBookResponse(string path, string id) {
        try {
            var response = await Config.Client.GetFromJsonAsync<BookmateBookResponse>($"https://api.bookmate.ru/api/v5/{path}/{id}".AsUri());
            if (response.AudioBook?.LinkedBooks?.Length > 0) {
                return await Config.Client.GetFromJsonAsync<BookmateBookResponse>($"https://api.bookmate.ru/api/v5/books/{response.AudioBook.LinkedBooks[0]}".AsUri());
            }

            return response;
        } catch (HttpRequestException ex) {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) {
                throw new Exception("Авторизационный токен невалиден. Требуется обновление");
            }

            throw;
        }
    }
    
}