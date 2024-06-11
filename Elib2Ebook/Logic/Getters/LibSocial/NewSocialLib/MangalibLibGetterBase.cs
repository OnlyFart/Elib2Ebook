using System.Text;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.SocialLib;
using HtmlAgilityPack;

namespace Elib2Ebook.Logic.Getters.LibSocial.NewSocialLib; 

public abstract class MangalibLibGetterBase : NewLibSocialGetterBase {
    protected MangalibLibGetterBase(BookGetterConfig config) : base(config) { }
    
    protected override HtmlDocument ResponseToHtmlDoc(SocialLibBookChapter chapterResponse) {
        var sb = new StringBuilder();

        foreach (var page in chapterResponse.Pages) {
            var url = SystemUrl.MakeRelativeUri(page.Url.TrimStart('/'));
            sb.Append($"<img src=\"{url}\" />");
        }

        return sb.AsHtmlDoc();
    }
}