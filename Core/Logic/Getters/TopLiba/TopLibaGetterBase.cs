using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters.TopLiba; 

public abstract class TopLibaGetterBase : GetterBase {
    public TopLibaGetterBase(BookGetterConfig config) : base(config) { }

    protected override string GetId(Uri url) => url.GetSegment(2);

    protected abstract Seria GetSeria(HtmlDocument doc, Uri url);

    protected abstract Task<TempFile> GetCover(HtmlDocument doc, Uri uri);
    
    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = SystemUrl.MakeRelativeUri($"/books/{bookId}");
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

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri("/login"));
        var token = doc.QuerySelector("meta[name=_token]").Attributes["content"].Value;
        
        var response = await Config.Client.PostWithTriesAsync(SystemUrl.MakeRelativeUri("/login"), GetAuthData(token));
        doc = await response.Content.ReadAsStreamAsync().ContinueWith(t => t.Result.AsHtmlDoc());
        var helpBlock = doc.QuerySelector("input[type=email] + span.help-block");
        if (helpBlock == default) {
            Config.Logger.LogInformation("Успешно авторизовались");
        } else {
            throw new Exception($"Не удалось авторизоваться. {helpBlock.GetText()}"); 
        }
    }
    
    private FormUrlEncodedContent GetAuthData(string token) {
        var data = new Dictionary<string, string> {
            ["email"] = Config.Options.Login,
            ["password"] = Config.Options.Password,
            ["_token"] = token
        };

        return new FormUrlEncodedContent(data);
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("h2[itemprop=author] a");
        return new Author(a.GetText(), url.MakeRelativeUri(a.Attributes["href"].Value));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(Uri uri, string bookId, string token, string title) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }
            
        foreach (var id in await GetToc(bookId)) {
            var chapter = new Chapter();
            var content = await GetChapter(bookId, id, token);
            if (content.StartsWith("{\"status\":\"error\"")) {
                Config.Logger.LogInformation($"Часть {id} заблокирована");
                continue;
            }

            var doc = content.AsHtmlDoc();
            chapter.Title = (doc.GetTextBySelector("h1.capter-title") ?? title).ReplaceNewLine();

            doc.RemoveNodes("h1");
            chapter.Images = await GetImages(doc, uri);
            chapter.Content = doc.DocumentNode.InnerHtml;
            
            
            Config.Logger.LogInformation($"Загружаю главу {chapter.Title.CoverQuotes()}");

            result.Add(chapter);
        }

        return result;
    }

    private async Task<string> GetChapter(string bookId, string id, string token) {
        var data = await Config.Client.PostWithTriesAsync(SystemUrl.MakeRelativeUri($"/reader/{bookId}/chapter"), GetData(id, token));
        return await data.Content.ReadAsStringAsync();
    }
    
    private static FormUrlEncodedContent GetData(string chapterId, string token) {
        var data = new Dictionary<string, string> {
            ["chapter"] = chapterId,
            ["_token"] = token,
        };

        return new FormUrlEncodedContent(data);
    }

    private async Task<IEnumerable<string>> GetToc(string bookId) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri($"/reader/{bookId}"));
        var result = new Regex("capters: \\[(?<chapters>.*?)\\]").Match(doc.Text).Groups["chapters"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim('\"')).ToList();
        return SliceToc(result, c => c);
    }

    private static string GetToken(HtmlDocument doc) {
        return doc.QuerySelector("meta[name=_token]").Attributes["content"].Value;
    }
}