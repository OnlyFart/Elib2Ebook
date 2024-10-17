using System.Text;
using Core.Configs;
using Core.Extensions;
using Core.Types.SocialLib;
using HtmlAgilityPack;

namespace Core.Logic.Getters.LibSocial.NewSocialLib; 

public abstract class MangaLibGetterBase : NewLibSocialGetterBase {
    protected MangaLibGetterBase(BookGetterConfig config) : base(config) { }
    
    protected override HtmlDocument ResponseToHtmlDoc(SocialLibBookChapter chapterResponse) {
        var sb = new StringBuilder();

        foreach (var page in chapterResponse.Pages) {
            var url = SystemUrl.MakeRelativeUri(page.Url.TrimStart('/'));
            sb.Append($"<img src=\"{url}\" />");
        }

        return sb.AsHtmlDoc();
    }
}