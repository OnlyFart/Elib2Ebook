using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Common;
using Elib2Ebook.Types.Jaomix;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class JaomixGetter : GetterBase {
    public JaomixGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://jaomix.ru/");
    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);
        url = SystemUrl.MakeRelativeUri($"/category/{GetId(url)}/");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1"),
            Author = new Author("Jaomix")
        };
            
        return book;
    }

    private async Task<Uri> GetMainUrl(Uri url) {
        if (url.GetSegment(1) != "category") {
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
            return url.MakeRelativeUri(doc.QuerySelector("span.entry-category a").Attributes["href"].Value);
        }

        return url;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();

        var termId = doc.QuerySelector("div.like-but").Id;
        var pages = await GetToc(termId, url);
        var pages_count = pages.Count();

        foreach (var page in pages) {
            Console.WriteLine($"Загружаю главу {page}/{pages_count}");
            var chapter = new Chapter();
            var chapterJ = await GetChapter(termId, page);
            chapter.Images = await GetImages(chapterJ.Content.Rendered.AsHtmlDoc(), url);
            chapter.Content = chapterJ.Content.Rendered;
            chapter.Title = chapterJ.Title.Rendered;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<JaomixChapter> GetChapter(String termId, Int32 page) {
        var res = await Config.Client.GetWithTriesAsync(SystemUrl.MakeRelativeUri($"/wp-json/wp/v2/posts?categories={termId}&per_page=1&page={page}"));

        var chapters = await res.Content.ReadFromJsonAsync<JaomixChapter[]>();
        var chapter = chapters[0];

        var doc = chapter.Content.Rendered.AsHtmlDoc();
        // var sb = new StringBuilder();

        // var b = doc.QuerySelector("body");
        // Console.WriteLine("----");
        // Console.WriteLine("----");
        // Console.WriteLine($"{b}");
        // Console.WriteLine("----");
        // Console.WriteLine("----");
        
        // foreach (var node in doc.QuerySelector(" > *")) {
        //     if (node.Name != "br" && node.Name != "script" && !string.IsNullOrWhiteSpace(node.InnerHtml) && node.Attributes["class"]?.Value?.Contains("adblock-service") == null) {
        //         var tag = node.Name == "#text" ? "p" : node.Name;
        //         sb.Append(node.InnerHtml.HtmlDecode().CoverTag(tag));
        //     }
        // }
        // chapter.Content.Rendered = sb.ToString();
            
        return chapter;
    }

    private async Task<IEnumerable<Int32>> GetToc(String termId, Uri url) {
            
        var chapters = new List<Int32>();
        
        var res = await Config.Client.GetWithTriesAsync(SystemUrl.MakeRelativeUri($"/wp-json/wp/v2/posts?categories={termId}&per_page=1&page=1"));

        Console.WriteLine("Получаю оглавление");
        var res_pages = res.Headers.GetValues("X-WP-TotalPages").First();

        var range = Enumerable.Range(1, Int32.Parse(res_pages));

        chapters.AddRange(range);

        Console.WriteLine($"Получено {chapters.Count} глав");

        return SliceToc(chapters);
    }
        
    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.img-book img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }

    private static IEnumerable<UrlChapter> ParseChapters(HtmlDocument doc, Uri url) {
        return doc.QuerySelectorAll("div.hiddenstab a").Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.InnerText.Trim()));
    }
}