using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Misc;
using Core.Types.Book;
using Core.Types.BookYandex;
using Core.Types.Common;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters.BooksYandex;

public class BooksYandexBooksGetter(BookGetterConfig config) : BooksYandexGetterBase(config) {
    protected override string[] Paths => ["books", "audiobooks", "serials"];

    protected override async Task<IEnumerable<Chapter>> FillChapters(Book book, BooksYandexResponse response) {
        if (Config.Options.HasAdditionalType(AdditionalTypeEnum.Audio)) {
            book.AdditionalFiles.Add(AdditionalTypeEnum.Audio, await GetAudio(response));
        }

        var id = response.Book?.UUID ?? response.AudioBook?.LinkedBooks?.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(id)) {
            return [];
        }
        
        if (response.Book?.SourceType == "serial") {
            return await GetFromSerial(book, id);
        }
        
        var requestUri = $"https://api.bookmate.ru/api/v5/books/{id}/content/v4".AsUri();
        var epubResponse = await Config.Client.GetAsync(requestUri);
        if (epubResponse.StatusCode == HttpStatusCode.OK) {
            var epubFile = await TempFile.Create(requestUri, Config.TempFolder.Path, epubResponse.Content.Headers.ContentDisposition.FileName.Trim('\"'), await epubResponse.Content.ReadAsStreamAsync());

            if (epubFile != default && Config.Options.HasAdditionalType(AdditionalTypeEnum.Books)) {
                book.AdditionalFiles.Add(AdditionalTypeEnum.Books, epubFile);
            }

            return await FillChaptersFromEpub(epubFile);
        }

        return [];
    }

    private async Task<IEnumerable<Chapter>> GetFromSerial(Book book, string id) {
        var episodes = await GetEpisodes(id);

        var result = new List<Chapter>();
        
        foreach (var episode in episodes.Where(e => e.CanBeRead)) {
            Config.Logger.LogInformation($"Загружаю эпизод {episode.Title.CoverQuotes()}");
            
            var requestUri = $"https://api.bookmate.ru/api/v5/books/{episode.Uuid}/content/v4".AsUri();
            var epubResponse = await Config.Client.GetAsync(requestUri);
            if (epubResponse.StatusCode != HttpStatusCode.OK) {
                continue;
            }

            var epubFile = await TempFile.Create(requestUri, Config.TempFolder.Path, epubResponse.Content.Headers.ContentDisposition.FileName.Trim('\"'), await epubResponse.Content.ReadAsStreamAsync());
        
            if (epubFile != default && Config.Options.HasAdditionalType(AdditionalTypeEnum.Books)) {
                book.AdditionalFiles.Add(AdditionalTypeEnum.Books, epubFile);
            }

            var episodeChapters = await FillChaptersFromEpub(epubFile);
            
            var chapter = new Chapter {
                Title = episode.Title,
                Content = string.Join(Environment.NewLine, episodeChapters.Select(c => c.Content)),
                Images = episodeChapters.SelectMany(c => c.Images)
            };
            
            result.Add(chapter);
        }

        return result;
    }
    
    private async Task<IEnumerable<BooksYandexEpisode>> GetEpisodes(string id) {
        try {
            var result = new List<BooksYandexEpisode>();

            for (var i = 1;; i++) {
                var response = await Config.Client.GetFromJsonAsync<BooksYandexEpisodes>($"https://api.bookmate.ru/api/v5/books/{id}/episodes".AsUri().AppendQueryParameter("page", i));
                if (response.Episodes.Length == 0) {
                    break;
                }
                
                result.AddRange(response.Episodes);
            }
            
            return result;
        } catch (HttpRequestException ex) {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) {
                throw new Exception("Авторизационный токен невалиден. Требуется обновление");
            }

            throw;
        }
    }
    
    private async Task<List<TempFile>> GetAudio(BooksYandexResponse bookResponse) {
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
    
    private async Task<BooksYandexPlaylist> GetPlayList(string id) {
        try {
            return await Config.Client.GetFromJsonAsync<BooksYandexPlaylist>($"https://api.bookmate.ru/api/v5/audiobooks/{id}/playlists.json".AsUri());
        } catch (HttpRequestException ex) {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) {
                throw new Exception("Авторизационный токен невалиден. Требуется обновление");
            }

            throw;
        }
    }
}