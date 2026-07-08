using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elib2Ebook.DomainServices.Configs;
using Elib2Ebook.DomainServices.Extensions;
using Elib2Ebook.ExternalServices.Renovels.Types;
using HtmlAgilityPack;

namespace Elib2Ebook.ExternalServices.Renovels.Getters;

public class RemangaGetter(BookGetterConfig config) : RenovelsGetterBase(config)
{
    protected override Uri SystemUrl => new("https://remanga.org/");

    protected override string Segment => "manga";

    protected override HtmlDocument GetChapterAsHtml(RenovelsChapter response)
    {
        var sb = new StringBuilder();

        foreach (var obj in response.Pages)
        {
            switch (obj)
            {
                case JsonObject:
                    sb.Append($"<img src='{obj.Deserialize<RenovelsPage>().Link}'/>");
                    break;
                case JsonArray pages:
                {
                    foreach (var page in pages)
                    {
                        sb.Append($"<img src='{page.Deserialize<RenovelsPage>().Link}'/>");
                    }

                    break;
                }
            }
        }

        return sb.AsHtmlDoc();
    }
}
