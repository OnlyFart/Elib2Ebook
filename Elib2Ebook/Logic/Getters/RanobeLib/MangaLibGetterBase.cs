using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.MangaLib;
using Elib2Ebook.Types.RanobeLib;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters.RanobeLib; 

public abstract class MangaLibGetterBase : RanobeLibGetterBase {
    protected MangaLibGetterBase(BookGetterConfig config) : base(config) { }

    private static IEnumerable<MangaLibPg> GetPg(HtmlDocument doc) {
        var match = new Regex("window.__pg = (?<data>.*);", RegexOptions.Compiled | RegexOptions.Singleline).Match(doc.QuerySelector("#pg").InnerText).Groups["data"].Value;
        var pg = match.Deserialize<MangaLibPg[]>();
        return pg.OrderBy(p => p.P);
    }

    protected override async Task<HtmlDocument> GetChapter(Uri url, RanobeLibChapter chapter) {
        var chapterDoc = await Config.Client.GetHtmlDocWithTriesAsync(url.AppendSegment($"/v{chapter.ChapterVolume}/c{chapter.ChapterNumber}?bid={chapter.BranchId}"));
        var header = chapterDoc.QuerySelector("div.auth-form-title");
        if (header != default && header.GetText() == "Авторизация") {
            throw new Exception("Произведение доступно только зарегистрированным пользователям. Добавьте в параметры вызова свои логин и пароль");
        }
        
        var pg = GetPg(chapterDoc);
        var sb = new StringBuilder();

        foreach (var p in pg) {
            var imageUrl = GetImageUrl(url, chapter, p);
            sb.Append($"<img src='{imageUrl}'/>");
        }

        return sb.AsHtmlDoc();
    }

    protected virtual string GetImageUrl(Uri url, RanobeLibChapter chapter, MangaLibPg p) {
        return $"https://img3.cdnlib.link/manga/{GetId(url)}/chapters/{chapter.ChapterSlug}/{p.U}";
    }
}