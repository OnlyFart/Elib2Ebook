using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.SocialLib;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters.LibSocial; 

public class RanobeLibGetter : LibSocialGetterBase {
    public RanobeLibGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://old.ranobelib.me/");
    protected override string GetId(Uri url) => url.GetSegment(1) + '/' + url.GetSegment(2);

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

    public override WindowData GetData(HtmlDocument doc)
    {
        var match = new Regex("window.__CHAPTERS__\\s*=\\s*(?<data>.*);\\s*window.__BRANCHES__", RegexOptions.Compiled | RegexOptions.Singleline)
            .Match(doc.Text)
            .Groups["data"]
            .Value;
        List<SocialLibChapterOld> socialLibChaptersOld = match.Deserialize<List<SocialLibChapterOld>>();
        var socialLibChapters = new SocialLibChapters
        {
            List = socialLibChaptersOld.ConvertAll(x => new SocialLibChapter()
            {
                ChapterNumber = x.ChapterNumber,
                ChapterVolume = Int32.Parse(x.ChapterVolume),
                ChapterName = x.ChapterName,
                ChapterSlug = x.ChapterName
            })
        };
        var windowData = new WindowData
        {
            Chapters = socialLibChapters,
        };
        //windowData.Chapters.List.Reverse();
        return windowData;
    }
}