using System.Text;
using Elib2Ebook.DomainServices.Configs;
using Elib2Ebook.DomainServices.Extensions;
using Elib2Ebook.ExternalServices.LibSocial.Types.SocialLib;
using HtmlAgilityPack;

namespace Elib2Ebook.ExternalServices.LibSocial.Getters;

public abstract class MangaLibGetterBase(BookGetterConfig config) : NewLibSocialGetterBase(config)
{
    protected override HtmlDocument ResponseToHtmlDoc(SocialLibBookChapter chapterResponse)
    {
        var sb = new StringBuilder();

        foreach (var page in chapterResponse.Pages)
        {
            var url = ImagesHost.MakeRelativeUri(page.Url.TrimStart('/'));
            sb.Append($"<img src=\"{url}\" />");
        }

        return sb.AsHtmlDoc();
    }
}
