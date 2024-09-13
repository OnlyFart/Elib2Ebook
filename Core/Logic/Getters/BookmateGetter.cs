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
        url = SystemUrl.MakeRelativeUri($"/books/{id}");
        
        var details = await GetBook(id);
        var book = new Book(url) {
            Cover = await GetCover(details),
            Title = details.Title,
            Author = GetAuthor(details),
            CoAuthors = GetCoAuthors(details),
            Annotation = details.Annotation,
            Lang = details.Language
        };
        
        var response = await Config.Client.GetAsync($"https://api.bookmate.ru/api/v5/books/{id}/content/v4");
        book.OriginalFile = new ShortFile(response.Content.Headers.ContentDisposition.FileName.Trim('\"'), await response.Content.ReadAsByteArrayAsync());
        book.Chapters = await FillChapters(book.OriginalFile);
        
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

    private async Task<IEnumerable<Chapter>> FillChapters(ShortFile file) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        var epubBook = EpubReader.Read(file.Bytes, Encoding.UTF8);
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

    private async Task<IEnumerable<Image>> GetImages(HtmlDocument doc, EpubBook book) {
        var images = new List<Image>();
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
    
    private Task<Image> GetCover(BookmateBook book) {
        var url = book.Cover.Large ?? book.Cover.Small;
        return !string.IsNullOrWhiteSpace(url) ? SaveImage(SystemUrl.MakeRelativeUri(url)) : Task.FromResult(default(Image));
    }
    
    private Author GetAuthor(BookmateBook book) {
        var author = book.Authors.FirstOrDefault();
        return new Author(author.Name, SystemUrl.MakeRelativeUri($"/authors/{author.Uuid}"));
    }
    
    private IEnumerable<Author> GetCoAuthors(BookmateBook book) {
        return book.Authors.Skip(1).Select(author => new Author(author.Name, SystemUrl.MakeRelativeUri($"/authors/{author.Uuid}"))).ToList();
    }
    
    private async Task<BookmateBook> GetBook(string id) {
        try {
            var response = await Config.Client.GetFromJsonAsync<BookmateBookResponse>($"https://api.bookmate.ru/api/v5/books/{id}".AsUri());
            return response.Book;
        } catch (HttpRequestException ex) {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) {
                throw new Exception("Авторизационный токен невалиден. Требуется обновление");
            }

            throw;
        }
    }
    
}