using System.Text.Json;
using System.Text.Json.Nodes;
using Elib2Ebook.DomainServices.Configs;
using Elib2Ebook.DomainServices.Extensions;
using Elib2Ebook.ExternalServices.LibSocial.Types.SocialLib;
using HtmlAgilityPack;

namespace Elib2Ebook.ExternalServices.LibSocial.Getters;

public class RanobeLibGetter(BookGetterConfig config) : NewLibSocialGetterBase(config)
{
    protected override Uri SystemUrl => new("https://ranobelib.me/");

    protected override int SiteId => 3;

    protected override HtmlDocument ResponseToHtmlDoc(SocialLibBookChapter chapterResponse)
    {
        return chapterResponse.Content switch
        {
            JsonValue e => e.GetValue<string>().AsHtmlDoc(),
            JsonObject o => AsHtml(chapterResponse.Attachments, o.Deserialize<SocialLibChapterContent>().Content).AsHtmlDoc(),
            _ => throw new Exception("Неизвестный тип"),
        };
    }
}
