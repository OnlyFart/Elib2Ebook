using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public abstract class GetterBase : IDisposable {
    protected readonly BookGetterConfig Config;

    protected GetterBase(BookGetterConfig config) {
        Config = config;
    }

    protected abstract Uri SystemUrl { get; }

    protected virtual string GetId(Uri url) {
        return url.Segments.Last().Trim('/');
    }

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
    protected async Task<Image> GetImage(Uri uri) {
        Console.WriteLine($"Загружаю картинку {uri.ToString().CoverQuotes()}");
        try {
            using var response = await Config.Client.SendWithTriesAsync(() => GetImageRequestMessage(uri));
            if (response is { StatusCode: HttpStatusCode.OK }) {
                return new Image(await response.Content.ReadAsByteArrayAsync()) {
                    Path = uri.GetFileName()
                };
            }

            return null;
        } catch (Exception) {
            return null;
        }
    }
        
    /// <summary>
    /// Загрузка изображений отдельной части
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="baseUri"></param>
    protected async Task<IEnumerable<Image>> GetImages(HtmlDocument doc, Uri baseUri) {
        var images = new ConcurrentBag<Image>();
        var toRemove = new ConcurrentBag<HtmlNode>();
        await Parallel.ForEachAsync(doc.QuerySelectorAll("img, image"), async (img, _) => {
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

            var image = await GetImage(uri);
            if (image?.Content == null || image.Content.Length == 0) {
                toRemove.Add(img);
                return;
            }

            img.Attributes.RemoveAll();
            img.Attributes.Add("src", image.Path);
            images.Add(image);
        });

        foreach (var node in toRemove) {
            node.Remove();
        }

        return images;
    }

    public virtual Task Authorize() {
        return Task.CompletedTask;
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