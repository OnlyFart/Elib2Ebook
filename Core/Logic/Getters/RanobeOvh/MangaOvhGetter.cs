using System;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.RanobeOvh;
using HtmlAgilityPack;

namespace Core.Logic.Getters.RanobeOvh; 

public class MangaOvhGetter(BookGetterConfig config) : RanobeOvhGetterBase(config) {
    protected override Uri SystemUrl => new("https://manga.ovh/");

    protected override async Task<HtmlDocument> GetChapter(RanobeOvhChapterShort ranobeOvhChapterFull) {
        var data = await Config.Client.GetFromJsonAsync<RanobeOvhChapterFull>($"https://api.{SystemUrl.Host}/chapter/{ranobeOvhChapterFull.Id}");
        var sb = new StringBuilder();

        foreach (var page in data.Pages) {
            sb.Append($"<img src='{page.Image}'/>");
        }

        return sb.AsHtmlDoc();
    }
}