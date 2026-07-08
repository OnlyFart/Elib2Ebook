using Elib2Ebook.DomainServices.Configs;
using Elib2Ebook.DomainServices.Extensions;
using Elib2Ebook.ExternalServices.Renovels.Types;
using HtmlAgilityPack;

namespace Elib2Ebook.ExternalServices.Renovels.Getters;

public class RenovelsGetter(BookGetterConfig config) : RenovelsGetterBase(config)
{
    protected override Uri SystemUrl => new("https://renovels.org/");

    protected override string Segment => "novel";

    protected override HtmlDocument GetChapterAsHtml(RenovelsChapter response)
    {
        return response.Content.AsHtmlDoc();
    }
}
