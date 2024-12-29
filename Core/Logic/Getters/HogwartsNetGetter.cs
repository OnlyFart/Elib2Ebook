using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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

public class HogwartsNetGetter(BookGetterConfig config) : GetterBase(config) {
    protected override Uri SystemUrl => new("https://hogwartsnet.ru/");
    
    private static Encoding _encoding = Encoding.GetEncoding(1251);

    protected override string GetId(Uri url) => url.GetQueryParameter("fid");

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }

        var payload = new Dictionary<string, string> {
            ["id"] = Config.Options.Login,
            ["pwd"] = Config.Options.Password
        };

        var doc = await Config.Client.PostHtmlDocWithTriesAsync("https://hogwartsnet.ru/mfanf/member.php".AsUri(), new FormUrlEncodedContent(payload), _encoding);
        var anketa = doc.QuerySelector("tr.top_fanf a[href*=anketa.php]");
        
        if (anketa == default) {
            throw new Exception("Не удалось авторизоваться. Проверьте правильность ID/пароля");
        }

        Config.Logger.LogInformation("Успешно авторизовались");
    }

    public override async Task<Book> Get(Uri url) {
        url = $"https://hogwartsnet.ru/mfanf/ffshowfic.php?fid={GetId(url)}".AsUri();
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url, _encoding);

        var book = new Book(url) {
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("td.fichead a[href*=ffshowfic.php]"),
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("td.fichead div")?.InnerHtml,
            Seria = GetSeria(doc, url)
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        foreach (var urlChapter in GetToc(doc, url)) {
            Config.Logger.LogInformation($"Загружаю главу {urlChapter.Title.CoverQuotes()}");
            var chapter = new Chapter {
                Title = urlChapter.Title
            };

            var chapterDoc = await GetChapter(urlChapter.Url);
            if (chapterDoc != default) {
                chapter.Images = await GetImages(chapterDoc, urlChapter.Url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }
            
            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url, _encoding);

        const string mature = "Текст фанфика доступен только зарегистрированным пользователям старше 18 лет";
        if (doc.ParsedText.Contains(mature)) {
            throw new Exception($"{mature}. Добавьте авторизацию по ID и паролю.");
        }

        var text = new StringBuilder();
        using var sr = new StringReader(doc.QuerySelector("#chap_text").RemoveNodes("center, div[id*=yandex]").InnerText.HtmlDecode());
        while (true) {
            var line = await sr.ReadLineAsync();
            if (line == null) {
                break;
            }

            if (string.IsNullOrWhiteSpace(line)) {
                continue;
            }
                
            text.Append(line.HtmlEncode().CoverTag("p"));
        }

        return text.AsHtmlDoc();
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        var result = new List<UrlChapter>();
        
        foreach (var option in doc.QuerySelectorAll("td.fichead_chapters select[name=chapter] option")) {
            var chapterId = option.Attributes["value"].Value;
            var chapterUrl = url.AppendQueryParameter("chapter", chapterId);
            var chapterTitle = option.GetText().StartsWith("глава") ? $"Глава {chapterId}" : option.GetText();
            
            result.Add(new UrlChapter(chapterUrl, chapterTitle));
        }

        return SliceToc(result, c => c.Title);
    }

    private static Seria GetSeria(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("td.fichead a[href*=findex.php]");
        if (a != default) {
            return new Seria {
                Name = a.GetText(),
                Url = url.MakeRelativeUri(a.Attributes["href"].Value)
            };
        }

        return default;
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("td.fichead a[href*=member.php]");
        return a != default ? 
            new Author(a.GetText().ReplaceNewLine(), url.MakeRelativeUri(a.Attributes["href"].Value)) : 
            new Author("HogwartsNet");
    }
}