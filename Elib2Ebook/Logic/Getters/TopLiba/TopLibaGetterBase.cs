using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters.TopLiba; 

public abstract class TopLibaGetterBase : GetterBase {
    public TopLibaGetterBase(BookGetterConfig config) : base(config) { }

    protected override string GetId(Uri url) {
        return url.Segments[2].Trim('/');
    }
    
    protected abstract Seria GetSeria(HtmlDocument doc, Uri url);

    protected abstract Task<Image> GetCover(HtmlDocument doc, Uri uri);
    
    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = new Uri($"https://{SystemUrl.Host}/books/{bookId}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        var title = doc.GetTextBySelector("h1[itemprop=name]");

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(url, bookId, GetToken(doc), title),
            Title = title,
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div.description")?.InnerHtml,
            Seria = GetSeria(doc, url)
        };
            
        return book;
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("h2[itemprop=author] a");
        return new Author(a.GetText(), new Uri(url, a.Attributes["href"].Value));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(Uri uri, string bookId, string token, string title) {
        var result = new List<Chapter>();
            
        foreach (var id in await GetChapterIds(bookId)) {
            var chapter = new Chapter();
            var content = await GetChapter(bookId, id, token);
            if (content.StartsWith("{\"status\":\"error\"")) {
                Console.WriteLine($"Часть {id} заблокирована");
                continue;
            }

            var doc = content.AsHtmlDoc();
            chapter.Title = (doc.GetTextBySelector("h1.capter-title") ?? title).ReplaceNewLine();

            doc.RemoveNodes("h1");
            chapter.Images = await GetImages(doc, uri);
            chapter.Content = doc.DocumentNode.InnerHtml;
            
            
            Console.WriteLine($"Загружаю главу {chapter.Title.CoverQuotes()}");

            result.Add(chapter);
        }

        return result;
    }

    private async Task<string> GetChapter(string bookId, string id, string token) {
        var data = await Config.Client.PostWithTriesAsync(new Uri($"https://{SystemUrl.Host}/reader/{bookId}/chapter"), GetData(id, token));
        return await data.Content.ReadAsStringAsync();
    }
    
    private static FormUrlEncodedContent GetData(string chapterId, string token) {
        var data = new Dictionary<string, string> {
            ["chapter"] = chapterId,
            ["_token"] = token,
        };

        return new FormUrlEncodedContent(data);
    }

    private async Task<IEnumerable<string>> GetChapterIds(string bookId) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://{SystemUrl.Host}/reader/{bookId}"));
        return new Regex("capters: \\[(?<chapters>.*?)\\]").Match(doc.Text).Groups["chapters"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim('\"'));
    }

    private static string GetToken(HtmlDocument doc) {
        return doc.QuerySelector("meta[name=_token]").Attributes["content"].Value;
    }
}