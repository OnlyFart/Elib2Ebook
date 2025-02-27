using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using EpubSharp;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public abstract class GetterBase(BookGetterConfig config) : IDisposable {
    protected readonly BookGetterConfig Config = config;

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
    protected async Task<TempFile> SaveImage(Uri uri) {
        try {
            Config.Logger.LogInformation($"Загружаю картинку {uri}");
            using var response = await Config.Client.SendWithTriesAsync(() => GetImageRequestMessage(uri));
            Config.Logger.LogInformation($"Загружена картинка {response.RequestMessage!.RequestUri}");
            if (response is not { StatusCode: HttpStatusCode.OK }) {
                Console.WriteLine(response.StatusCode);
                return default;
            }
            
            await using var stream = await response.Content.ReadAsStreamAsync();

            var extension = Path.GetExtension(uri.GetFileName());
            var fullName = Guid.NewGuid() + (string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension); 
            
            return await TempFile.Create(uri, Config.TempFolder.Path, fullName, stream);
        } catch (Exception ex) {
            Console.WriteLine(ex);
            return default;
        }
    }
        
    /// <summary>
    /// Загрузка изображений отдельной части
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="baseUri"></param>
    protected async Task<IEnumerable<TempFile>> GetImages(HtmlDocument doc, Uri baseUri) {
        var images = new ConcurrentBag<Tuple<TempFile, int>>();
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
            img.Attributes.Add("src", image.FullName);
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

    private static int GetIndexByName<T>(ICollection<T> toc, Func<T, string> selector, string name) {
        var result = toc
            .Select((chapter, index) => new { chapter, index })
            .FirstOrDefault(c => string.Equals(selector(c.chapter)?.Trim(), name, StringComparison.InvariantCultureIgnoreCase))
            ?.index;

        if (!result.HasValue) {
            throw new Exception($"Глава с названием {name.CoverQuotes()} не найдена");
        }
        
        return result.Value + 1;
    }
    
    private static HtmlDocument SliceBook(EpubBook epubBook, EpubChapter epubChapter) {
        var doc = new HtmlDocument();

        var startChapter = epubBook.Resources.Html.First(h => HttpUtility.UrlDecode(h.AbsolutePath) == HttpUtility.UrlDecode(epubChapter.AbsolutePath));
        var startIndex = epubBook.Resources.Html.IndexOf(startChapter);
        
        var chapter = epubBook.Resources.Html[startIndex].TextContent.AsHtmlDoc();
        foreach (var node in chapter.QuerySelector("body").ChildNodes) {
            doc.DocumentNode.AppendChild(node);
        }
        
        for (var i = startIndex + 1; i < epubBook.Resources.Html.Count; i++) {
            var chapterContent = epubBook.Resources.Html[i];
            if (chapterContent.AbsolutePath == epubChapter.Next?.AbsolutePath) {
                break;
            }
            
            chapter = chapterContent.TextContent.AsHtmlDoc();
            foreach (var node in chapter.QuerySelector("body").ChildNodes) {
                doc.DocumentNode.AppendChild(node);
            }
        }
        
        return doc;
    }
    
    protected async Task<IEnumerable<Chapter>> FillChaptersFromEpub(TempFile shortFile) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        await using var stream = shortFile.GetStream();
        var epubBook = EpubReader.Read(stream, true, Encoding.UTF8);

        if (epubBook.TableOfContents.Count > 0) {
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
        } else {
            Config.Logger.LogInformation($"Загружаю главу {epubBook.Title.CoverQuotes()}");

            var sb = new StringBuilder();
            foreach (var textFile in epubBook.SpecialResources.HtmlInReadingOrder) {
                var textFileDoc = textFile.TextContent.AsHtmlDoc();
                var body = textFileDoc.DocumentNode.QuerySelector("body");
                if (body != default) {
                    textFileDoc = body.InnerHtml.AsHtmlDoc();
                }

                sb.Append(textFileDoc.DocumentNode.InnerHtml);

            }
            
            var chapter = new Chapter {
                Title = epubBook.Title
            };
            
            var content = sb.AsHtmlDoc();
            chapter.Images = await GetImages(content, epubBook);
            chapter.Content = content.DocumentNode.InnerHtml;
            result.Add(chapter);
        }

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

    protected IEnumerable<T> SliceToc<T>(ICollection<T> toc, Func<T, string> selector) {
        var start = Config.Options.Start;
        var end = Config.Options.End;

        var startName = (Config.Options.StartName ?? string.Empty).Trim();
        var endName = (Config.Options.EndName ?? string.Empty).Trim();

        if (!string.IsNullOrWhiteSpace(startName)) {
            start = GetIndexByName(toc, selector, startName);
        }
        
        if (!string.IsNullOrWhiteSpace(endName)) {
            end = GetIndexByName(toc, selector, endName);
        }

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

    private static HtmlDocument GetContent(EpubBook epubBook, EpubChapter epubChapter) {
        var chapter = new HtmlDocument();

        var book = SliceBook(epubBook, epubChapter);
        var startNode = string.IsNullOrWhiteSpace(epubChapter.HashLocation) ? book.DocumentNode : book.QuerySelector($"#{epubChapter.HashLocation}");
        var needStop = false;

        var layer = (startNode.ParentNode ?? startNode).CloneNode(false);
        do {
            var clone = CloneNode(startNode, epubChapter.Next?.HashLocation, ref needStop);
            if (clone != default) {
                layer.AppendChild(clone);
            }

            do {
                if (startNode.NextSibling == default) {
                    if (startNode.ParentNode == default || startNode.ParentNode.Name == "#document") {
                        startNode = default;
                    } else {
                        var layerClone = layer.CloneNode(true);
                        layer = startNode.ParentNode.CloneNode(false);
                        layer.AppendChild(layerClone);
                        startNode = startNode.ParentNode;
                    }
                } else {
                    startNode = startNode.NextSibling;
                    break;
                }
            } while (startNode != default);
        } while (startNode != default && !needStop);

        chapter.DocumentNode.AppendChild(layer);

        return chapter;
    }

    private static HtmlNode CloneNode(HtmlNode node, string stopId, ref bool needStop) {
        if (!string.IsNullOrWhiteSpace(stopId) && node.Name != "#document" && node.Id == stopId) {
            needStop = true;
            return default;
        }


        if (!node.HasChildNodes) {
            return node.CloneNode(true);
        }

        var parent = node.CloneNode(false);

        foreach (var child in node.ChildNodes) {
            var clone = CloneNode(child, stopId, ref needStop);

            if (clone != default) {
                parent.ChildNodes.Add(clone);
            }

            if (needStop) {
                return parent;
            }
        }

        return parent;
    }

    public abstract Task<Book> Get(Uri url);
    public virtual Task<List<object>> GetTocVolumized(Uri url) {
        var x = new List<object>();
        return Task.FromResult(x);
    }
        
    public virtual Task Init() {
        Config.Client.DefaultRequestVersion = HttpVersion.Version20;
        Config.Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.0 Safari/605.1.15");
        Config.Client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        Config.Client.DefaultRequestHeaders.Add("Accept-Language", "ru");
        Config.Client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        
        return Task.CompletedTask;
    }

    public virtual void Dispose() {
        Config?.Dispose();
    }
}