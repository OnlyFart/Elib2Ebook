using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public abstract class GetterBase : IDisposable {
    protected readonly BookGetterConfig Config;

    protected GetterBase(BookGetterConfig config) {
        Config = config;
    }

    protected abstract Uri SystemUrl { get; }

    protected virtual string GetId(Uri url) => url.Segments.Last().Trim('/');

    public virtual bool IsSameUrl(Uri url) {
        return SystemUrl.IsSameHost(url);
    }

    protected virtual HttpRequestMessage GetImageRequestMessage(Uri uri) {
        var message = new HttpRequestMessage(HttpMethod.Get, uri);
        message.Version = Config.Client.DefaultRequestVersion;

        foreach (var header in Config.Client.DefaultRequestHeaders) {
            message.Headers.Add(header.Key, header.Value);
        }
        
        return message;
    }
    
    /// <summary>
    /// Получение изображения
    /// </summary>
    /// <param name="uri"></param>
    /// <returns></returns>
    protected async Task<Image> SaveImage(Uri uri) {
        try {
            Config.Logger.LogInformation($"Загружаю картинку {uri}");
            using var response = await Config.Client.SendWithTriesAsync(() => GetImageRequestMessage(uri));
            Config.Logger.LogInformation($"Загружена картинка {response.RequestMessage!.RequestUri}");
            if (response is not { StatusCode: HttpStatusCode.OK }) {
                return default;
            }
            
            await using var stream = await response.Content.ReadAsStreamAsync();
            return await Image.Create(uri, Config.TempFolder.Path, uri.GetFileName(), stream);

        } catch (Exception) {
            return default;
        }
    }
        
    /// <summary>
    /// Загрузка изображений отдельной части
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="baseUri"></param>
    protected async Task<IEnumerable<Image>> GetImages(HtmlDocument doc, Uri baseUri) {
        var images = new ConcurrentBag<Tuple<Image, int>>();
        var toRemove = new ConcurrentBag<HtmlNode>();
        var tuples = doc.QuerySelectorAll("img, image").Select((img, i) => Tuple.Create(img, i));
        await Parallel.ForEachAsync(tuples, async (t, _) => {
            var img = t.Item1;
            
            if (Config.Options.NoImage) {
                toRemove.Add(img);
                return;
            }
            
            var path = img.Attributes["src"]?.Value ?? img.Attributes["data-src"]?.Value;
            if (string.IsNullOrWhiteSpace(path)) {
                toRemove.Add(img);
                return;
            }

            if (!Uri.TryCreate(baseUri, path, out var uri)) {
                toRemove.Add(img);
                return;
            }

            var image = await SaveImage(uri);
            if (image == default) {
                toRemove.Add(img);
                return;
            }

            img.Attributes.RemoveAll();
            img.Attributes.Add("src", image.Name);
            images.Add(Tuple.Create(image, t.Item2));
        });

        foreach (var node in toRemove) {
            node.Remove();
        }

        return images.OrderBy(t => t.Item2).Select(t => t.Item1);
    }

    public virtual Task Authorize() {
        return Task.CompletedTask;
    }

    protected IEnumerable<T> SliceToc<T>(ICollection<T> toc) {
        var start = Config.Options.Start;
        var end = Config.Options.End;

        if (start.HasValue && end.HasValue) {
            var startIndex = start.Value >= 0 ? start.Value : toc.Count + start.Value + 1; 
            var endIndex = end.Value >= 0 ? end.Value : toc.Count + end.Value; 
            
            return toc.Skip(startIndex - 1).Take(endIndex - startIndex + 1);
        }
        
        if (start.HasValue) {
            var startIndex = start.Value >= 0 ? start.Value : toc.Count + start.Value + 1; 
            return toc.Skip(startIndex - 1);
        }
        
        if (end.HasValue) {
            var endIndex = end.Value >= 0 ? end.Value : toc.Count + end.Value; 
            return toc.Take(endIndex);
        }
        
        return toc;
    }

    public abstract Task<Book> Get(Uri url);
        
    public virtual Task Init() {
        Config.Client.DefaultRequestVersion = HttpVersion.Version20;
        Config.Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.0 Safari/605.1.15");
        Config.Client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        Config.Client.DefaultRequestHeaders.Add("Accept-Language", "ru");
        Config.Client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        
        return Task.CompletedTask;
    }

    public void Dispose() {
        Config?.Dispose();
    }
}