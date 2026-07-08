using System.Net.Http.Json;
using System.Text;
using Elib2Ebook.DomainServices.Configs;
using Elib2Ebook.DomainServices.Extensions;
using Elib2Ebook.ExternalServices.RanobeOvh.Types;
using HtmlAgilityPack;

namespace Elib2Ebook.ExternalServices.RanobeOvh.Getters;

public class MangaOvhGetter(BookGetterConfig config) : RanobeOvhGetterBase(config)
{
    protected override Uri SystemUrl => new("https://manga.ovh/");

    protected override async Task<HtmlDocument> GetChapter(RanobeOvhChapterShort ranobeOvhChapterFull)
    {
        var data = await Config.Client.GetFromJsonAsync<RanobeOvhChapterFull>($"https://api.{SystemUrl.Host}/chapter/{ranobeOvhChapterFull.Id}");
        var sb = new StringBuilder();

        foreach (var page in data.Pages)
        {
            sb.Append($"<img src='{page.Image}'/>");
        }

        return sb.AsHtmlDoc();
    }
}
