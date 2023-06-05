using System;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.RanobeOvh;
using HtmlAgilityPack;

namespace Elib2Ebook.Logic.Getters.RanobeOvh; 

public class MangaOvhGetter : RanobeOvhGetterBase {
    public MangaOvhGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://manga.ovh/");

    protected override string Segment => "manga";

    protected override async Task<HtmlDocument> GetChapter(RanobeOvhChapter ranobeOvhChapter) {
        var data = await Config.Client.GetFromJsonAsync<RanobeOvhChapter>($"https://api.{SystemUrl.Host}/chapter/{ranobeOvhChapter.Id}");
        var sb = new StringBuilder();

        foreach (var page in data.Pages) {
            sb.Append($"<img src='{page.Image}'/>");
        }

        return sb.AsHtmlDoc();
    }

    protected override T GetNextData<T>(HtmlDocument doc, string node) {
        var json = Regex.Match(doc.ParsedText, "__remixContext = (?<data>.*?);</script>", RegexOptions.Singleline).Groups["data"].Value;
        return JsonDocument.Parse(json)
            .RootElement.GetProperty("state")
            .GetProperty("loaderData")
            .GetProperty("routes/reader/book/$slug/index")
            .GetProperty(node)
            .GetRawText()
            .Deserialize<T>();
    }
}