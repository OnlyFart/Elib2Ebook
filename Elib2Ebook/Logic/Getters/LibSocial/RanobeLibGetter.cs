using System;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.SocialLib;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters.LibSocial; 

public class RanobeLibGetter : LibSocialGetterBase {
    public RanobeLibGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ranobelib.me");

    protected override async Task<HtmlDocument> GetChapter(Uri url, SocialLibChapter chapter, User user) {
        var segment = $"/v{chapter.ChapterVolume}/c{chapter.ChapterNumber}?bid={chapter.BranchId}";
        if (user != default) {
            segment += $"&ui={user.Id}";
        }

        var chapterDoc = await Config.Client.GetHtmlDocWithTriesAsync(url.AppendSegment(segment));
        var header = chapterDoc.QuerySelector("h2.page__title");
        if (header != default && header.GetText() == "Регистрация") {
            throw new Exception("Произведение доступно только зарегистрированным пользователям. Добавьте в параметры вызова свои логин и пароль");
        }

        return chapterDoc.QuerySelector("div.reader-container").InnerHtml.AsHtmlDoc();
    }
}