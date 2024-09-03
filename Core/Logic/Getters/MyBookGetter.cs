using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.MyBook;
using EpubSharp;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;
using OAuth;

namespace Core.Logic.Getters; 

public class MyBookGetter : GetterBase {
    public MyBookGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://mybook.ru/");

    private const string CONSUMER_KEY = "830968793b2a44c688400319f4a77231";
    private const string CONSUMER_SECRET = "HhaxXsMryrLz49R4";
    private const string IDENTIFIER = "47b17a4c8231a28f06c6c836d0b5f6f2134cb74d";

    private HttpClient _apiClient;
    private MyBookAuth _token = new() {
        Secret = "fYFPndYhOTW6YxIJ",
        Token = "f8d32ef906664ed3a1525b7298aac461"
    };

    private Uri GetMainUrl(Uri uri) {
        return SystemUrl.MakeRelativeUri($"{uri.GetSegment(1)}/{uri.GetSegment(2)}/{uri.GetSegment(3)}");
    }

    public override Task Init() {
        _apiClient = new HttpClient();
        _apiClient.DefaultRequestHeaders.Add("User-Agent", "MyBook/6.6.0 (iPhone; iOS 16.0.3; Scale/3.00)");
        _apiClient.DefaultRequestHeaders.Add("Accept", "application/json; version=4");
        return Task.CompletedTask;
    }

    private void SetAuthHeader(string method, Uri uri) {
        var client = new OAuthRequest {
            Method = method,
            SignatureMethod = OAuthSignatureMethod.HmacSha1,
            ConsumerKey = CONSUMER_KEY,
            ConsumerSecret = CONSUMER_SECRET,
            RequestUrl = uri.ToString(),
            Version = "1.0",
            Token = _token.Token,
            TokenSecret = _token.Secret
        };
        
        _apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", client.GetAuthorizationHeader().Replace("OAuth", string.Empty).Trim());
    }

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }
        
        var authUrl = SystemUrl.MakeRelativeUri("/api/auth/");
        SetAuthHeader("POST", authUrl);

        var payload = new {
            identifier = IDENTIFIER,
            email = Config.Options.Login,
            password = Config.Options.Password
        };
        
        using var response = await _apiClient.PostAsJsonAsync(authUrl, payload);
        if (response.StatusCode == HttpStatusCode.OK) {
            Config.Logger.LogInformation("Успешно авторизовались");
        } else {
            var message = await response.Content.ReadAsStringAsync();
            throw new Exception($"Не удалось авторизоваться. {message}");
        }
        
        _token = await response.Content.ReadFromJsonAsync<MyBookAuth>();
    }
    
    private static T GetNextData<T>(HtmlDocument doc, string node) {
        var json = doc.QuerySelector("#__NEXT_DATA__").InnerText;
        return JsonDocument.Parse(json)
            .RootElement.GetProperty("props")
            .GetProperty("initialProps")
            .GetProperty("pageProps")
            .GetProperty(node)
            .GetRawText()
            .Deserialize<T>();
    }

    public override async Task<Book> Get(Uri url) {
        url = GetMainUrl(url);
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        var details = GetNextData<MyBookBook>(doc, "book");
        
        var book = new Book(url) {
            Cover = await GetCover(details),
            Chapters = await FillChapters(details),
            Title = details.Name,
            Author = GetAuthor(details),
            Annotation = details.Annotation,
        };

        return book;
    }

    private static HtmlDocument SliceBook(EpubBook epubBook, EpubChapter epubChapter) {
        var doc = new HtmlDocument();

        var startChapter = epubBook.Resources.Html.First(h => h.AbsolutePath == epubChapter.AbsolutePath);
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

    private async Task<IEnumerable<Chapter>> FillChapters(MyBookBook details) {
        var chapters = new List<Chapter>();
        
        var bookUrl = SystemUrl.MakeRelativeUri(details.BookFile);
        
        SetAuthHeader("GET", bookUrl);
        using var response = await _apiClient.GetAsync(bookUrl);
        if (response.StatusCode != HttpStatusCode.OK) {
            throw new Exception("Не удалось получить книгу");
        }
        
        var epubBook = EpubReader.Read(await response.Content.ReadAsStreamAsync(), false, Encoding.UTF8);
        var current = epubBook.TableOfContents.First();
        
        do {
            Config.Logger.LogInformation($"Загружаю главу {current.Title.CoverQuotes()}");

            var chapter = new Chapter {
                Title = current.Title
            };

            var content = GetContent(epubBook, current);
            chapter.Images = await GetImages(content, epubBook);
            chapter.Content = content.DocumentNode.RemoveNodes("h1, h2, h3").InnerHtml;
            chapters.Add(chapter);
        } while ((current = current.Next) != default);

        return chapters;
    }

    private async Task<IEnumerable<Image>> GetImages(HtmlDocument doc, EpubBook book) {
        var images = new List<Image>();
        foreach (var img in doc.QuerySelectorAll("img")) {
            var path = img.Attributes["src"]?.Value;
            if (string.IsNullOrWhiteSpace(path)) {
                img.Remove();
                continue;
            }

            var t = book.Resources.Images.FirstOrDefault(i => i.Href == path);
            if (t == default) {
                img.Remove();
                continue;
            }

            if (t.Content == null || t.Content.Length == 0) {
                img.Remove();
                continue;
            }
            
            var image = await Image.Create(null, Config.TempFolder.Path, t.Href, t.Content);
            img.Attributes["src"].Value = image.Name;
            images.Add(image);
        }

        return images;
    }

    private static HtmlDocument GetContent(EpubBook epubBook, EpubChapter epubChapter) {
        var chapter = new HtmlDocument();

        var book = SliceBook(epubBook, epubChapter);
        var startNode = book.QuerySelector($"#{epubChapter.HashLocation}");
        var needStop = false;

        var layer = startNode.ParentNode.CloneNode(false);
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
        if (!string.IsNullOrWhiteSpace(stopId) && node.Id == stopId) {
            needStop = true;
            return default;
        }

        if (!node.HasChildNodes) {
            return node.CloneNode(true);
        }
        
        var parent = node.CloneNode(false);
            
        foreach (var child in node.ChildNodes) {
            var clone = CloneNode(child, stopId, ref needStop);
            if (needStop || clone == default) {
                return parent;
            }
                
            parent.ChildNodes.Add(clone);    
        }

        return parent;

    }

    private Task<Image> GetCover(MyBookBook book) {
        return !string.IsNullOrWhiteSpace(book.Cover) ? SaveImage($"https://i3.mybook.io/p/x378/{book.Cover.TrimStart('/')}".AsUri()) : Task.FromResult(default(Image));
    }
    
    private Author GetAuthor(MyBookBook book) {
        if (book.Authors.Length == 0) {
            return new Author("MyBook");
        }

        var author = book.Authors[0];
        return string.IsNullOrWhiteSpace(author.Url) ? new Author(author.Name) : new Author(author.Name, SystemUrl.MakeRelativeUri(author.Url));
    }
    
    public new void Dispose() {
        base.Dispose();
        _apiClient?.Dispose();
    }
}