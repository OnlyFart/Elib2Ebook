using System;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.RanobeLib;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters.RanobeLib; 

public class RanobeLibGetter : RanobeLibGetterBase {
    public RanobeLibGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ranobelib.me");

    protected override async Task<HtmlDocument> GetChapter(Uri url, RanobeLibChapter chapter) {
        var chapterDoc = await Config.Client.GetHtmlDocWithTriesAsync(url.AppendSegment($"/v{chapter.ChapterVolume}/c{chapter.ChapterNumber}?bid={chapter.BranchId}"));
        var header = chapterDoc.QuerySelector("h2.page__title");
        if (header != default && header.GetText() == "Регистрация") {
            throw new Exception("Произведение доступно только зарегистрированным пользователям. Добавьте в параметры вызова свои логин и пароль");
        }

        return chapterDoc.QuerySelector("div.reader-container").InnerHtml.AsHtmlDoc();
    }
}