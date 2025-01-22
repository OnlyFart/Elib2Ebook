using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class ReadliGetter(BookGetterConfig config) : GetterBase(config) {
    protected override Uri SystemUrl => new("https://readli.net");
    
    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        var pages = long.Parse(doc.GetTextBySelector("span.button-pages__right").Split(' ')[0]);
        var imageDiv = doc.QuerySelector("div.book-image");
        var href = url.MakeRelativeUri(imageDiv.QuerySelector("a").Attributes["href"].Value);
        var bookId = GetBookId(href);
            
        var title = doc.GetTextBySelector("h1.main-info__title");
        var book = new Book(url) {
            Cover = await GetCover(imageDiv, url),
            Chapters = await FillChapters(bookId, pages, title),
            Title = title,
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("article.seo__content")?.RemoveNodes("h2")?.InnerHtml,
            Seria = GetSeria(doc, url)
        };

        return book; 
    }
    
    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var author = doc.QuerySelector("a.main-info__link");
        return new Author(author.InnerText.HtmlDecode(), url.MakeRelativeUri(author.Attributes["href"]?.Value ?? string.Empty));
    }
    
    private static Seria GetSeria(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("div.book-info a.book-info__link[href^=/serie/]");
        if (string.IsNullOrWhiteSpace(a.GetText())) {
            return default;
        }

        var text = a.GetText();
        var uri = url.MakeRelativeUri(a.Attributes["href"].Value);
        if (!text.Contains('#')) {
            return new Seria {
                Name = text,
                Url = uri
            };
        }

        var parts = text.Split('#', StringSplitOptions.RemoveEmptyEntries);

        return new Seria {
            Name = parts[0].HtmlDecode(),
            Number = parts[1].HtmlDecode(),
            Url = uri
        };

    }

    private static string GetBookId(Uri uri) {
        return uri.GetQueryParameter("b") ?? throw new InvalidOperationException("Не удалось определить идентификатор книги");
    }

    private async Task<Uri> GetMainUrl(Uri url) {
        if (GetId(url).StartsWith("chitat-online", StringComparison.InvariantCultureIgnoreCase)) {
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri($"/chitat-online/?b={GetBookId(url)}"));
            var href = doc.QuerySelector("h1 a").Attributes["href"].Value;
            return url.MakeRelativeUri(href);
        }

        return url;
    }

    private Task<TempFile> GetCover(HtmlNode doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }

    private async Task AddChapter(ICollection<Chapter> chapters, Chapter chapter, StringBuilder text) {
        if (chapter == null) {
            return;
        }
        
        var chapterDoc = text.AsHtmlDoc();
        chapter.Images = await GetImages(chapterDoc, SystemUrl.MakeRelativeUri("/chitat-online/"));
        chapter.Content = chapterDoc.DocumentNode.InnerHtml;
        chapters.Add(chapter);
    }

    private static bool IsSingleChapter(IEnumerable<HtmlNode> nodes) {
        var firstNode = nodes.First();
        return firstNode.Name != "h3" || string.IsNullOrWhiteSpace(firstNode.InnerText);
    }

    private static Chapter CreateChapter(string title) {
        return new Chapter {
            Title = title
        };
    }

    private async Task<HtmlDocument> GetChapter(string bookId, int page) {
        var response = await Config.Client.GetWithTriesAsync(SystemUrl.MakeRelativeUri($"/chitat-online/?b={bookId}&pg={page}"));
        if (response == default) {
            return default;
        }

        return await response.Content.ReadAsStreamAsync().ContinueWith(t => t.Result.AsHtmlDoc());
    }

    private async Task<List<Chapter>> FillChapters(string bookId, long pages, string name) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }
        
        Chapter chapter = null;
        var singleChapter = true;
        var text = new StringBuilder();
            
        for (var i = 1; i <= pages; i++) {
            Config.Logger.LogInformation($"Получаю страницу {i}/{pages}");
            var page = await GetChapter(bookId, i);
            if (page == default) {
                Config.Logger.LogInformation("Неожиданный конец");
                break;
            }
            
            var content = page.QuerySelector("div.reading__text").RemoveNodes("script, ins, div[id*=adfox]");
            var nodes = content.QuerySelectorAll("> h3, > p, > img");
            nodes = nodes.Count == 0 ? content.ChildNodes : nodes;
            singleChapter = i == 1 ? IsSingleChapter(nodes) : singleChapter;

            foreach (var node in nodes) {
                if (singleChapter || node.Name != "h3") {
                    if (node.Name == "img" && node.Attributes["src"] != null) {
                        text.Append($"<img src='{node.Attributes["src"].Value}'/>");
                    } else {
                        if (!string.IsNullOrWhiteSpace(node.InnerHtml)) {
                            text.Append(node.InnerHtml.HtmlDecode().CoverTag("p"));
                        }
                    }
                } else {
                    await AddChapter(result, chapter, text);
                    text.Clear();
                    chapter = CreateChapter(node.InnerHtml.HtmlDecode());
                }
            }
        }

        await AddChapter(result, chapter ?? CreateChapter(name), text);
        return result;
    }
}