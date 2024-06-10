using System;
using System.Text;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.SocialLib;
using HtmlAgilityPack;

namespace Elib2Ebook.Logic.Getters.LibSocial.NewSocialLib; 

public class HentaiLibGetter : NewLibSocialGetterBase {
    public HentaiLibGetter(BookGetterConfig config) : base(config) { }
    
    protected override Uri SystemUrl => new("https://hentailib.me");

    protected override Uri ImagesHost => new("https://img3.imglib.info/");

    protected override HtmlDocument ResponseToHtmlDoc(SocialLibBookChapter chapterResponse) {
        var sb = new StringBuilder();

        foreach (var page in chapterResponse.Pages) {
            var url = SystemUrl.MakeRelativeUri(page.Url.TrimStart('/'));
            sb.Append($"<img src=\"{url}\" />");
        }

        return sb.AsHtmlDoc();
    }
}