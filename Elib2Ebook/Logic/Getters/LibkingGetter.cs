using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters;

public class LibkingGetter : GetterBase {
    public LibkingGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://libking.ru/");

    public override async Task<Book> Get(Uri url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var name = doc.GetTextBySelector("div[itemprop=name]");

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url, name),
            Title = name,
            Author = GetAuthor(doc),
            Annotation = doc.QuerySelector("div.mov-desc-text")?.InnerHtml,
        };

        return book;
    }

    private Author GetAuthor(HtmlDocument doc) {
        var a = doc.QuerySelector("a[itemprop=author]");
        return a == default ? new Author("LibKing") : new Author(a.GetText(), SystemUrl.MakeRelativeUri(a.Attributes["href"].Value));
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.mov-img img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }

    private async Task AddChapter(ICollection<Chapter> chapters, Chapter chapter, StringBuilder text) {
        var chapterDoc = text.AsHtmlDoc();
        chapter.Images = await GetImages(chapterDoc, SystemUrl);
        chapter.Content = chapterDoc.DocumentNode.InnerHtml;
        chapters.Add(chapter);
    }
    
    private static Chapter CreateChapter(string title) {
        return new Chapter {
            Title = title
        };
    }

    private Uri GetFirstPage(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("div.pages a.pagenav");
        if (a.GetText() != "1") {
            return url;
        }

        return url.MakeRelativeUri(a.Attributes["href"].Value);
    }

    private Uri GetNextPage(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("a.load_text");
        if (a == default) {
            return default;
        }

        return url.MakeRelativeUri(a.Attributes["href"].Value);
    }

    private bool IsTitle(HtmlNode node) {
        return node.Name == "div" && node.Attributes["class"]?.Value.Contains("title") == true;
    }

    private HtmlNode GetTitleNode(HtmlDocument doc) {
        foreach (var node in doc.DocumentNode.ChildNodes) {
            if (node.Name == "#text" && string.IsNullOrWhiteSpace(node.GetText())) {
                continue;
            }

            if (IsTitle(node)) {
                return node;
            }

            return default;
        }

        return default;
    }
    
    private async Task<List<Chapter>> FillChapters(HtmlDocument doc, Uri baseUrl, string name) {
        var chapters = new List<Chapter>();
        var fullText = new StringBuilder();
        var i = 1;

        for (var url = GetFirstPage(doc, baseUrl); url != default;) {
            Console.WriteLine($"Получаю страницу {i}");
            doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
            url = GetNextPage(doc, baseUrl);
            fullText.Append(doc.QuerySelector("#book").RemoveNodes(".navigation, .cpab, .cpab1, .load_text, #twentypersent").InnerHtml);
            i++;
        }
        
        var book = fullText.AsHtmlDoc();
        var node = GetTitleNode(book);
        if (node != default) {
            while (node != default) {
                var title = node.GetText();
                var chapterText = new StringBuilder();

                do {
                    node = node.NextSibling;
                    chapterText.Append(node.OuterHtml);
                } while (node.NextSibling != default && !IsTitle(node.NextSibling));

                node = node.NextSibling;
                await AddChapter(chapters, CreateChapter(title), chapterText);
            }
        } else {
            await AddChapter(chapters, CreateChapter(name), fullText);
        }
        
        return chapters;
    }
}