using System;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.RanobeOvh;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters.RanobeOvh; 

public class RanobeOvhGetter : RanobeOvhGetterBase {
    public RanobeOvhGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ranobe.ovh/");

    protected override string Segment => "ranobe";

    protected override async Task<HtmlDocument> GetChapter(RanobeOvhChapter ranobeOvhChapter) {
        var data = await Config.Client.GetFromJsonAsync<RanobeOvhChapter>($"https://api.{SystemUrl.Host}/chapter/{ranobeOvhChapter.Id}");
        var sb = new StringBuilder();

        foreach (var page in data.Pages) {
            switch (page.Metadata.Type) {
                case "paragraph":
                    sb.Append(page.Text.HtmlDecode().CoverTag("p"));
                    break;
                case "image":
                    sb.Append($"<img src='{page.Image}'/>");
                    break;
                case "delimiter":
                    sb.Append("***".CoverTag("h3"));
                    break;
                default:
                    Console.WriteLine($"Неизвестный тип: {page.Metadata.Type}");
                    sb.Append(page.Text.HtmlDecode().CoverTag("p"));
                    break;
            }
        }

        return sb.AsHtmlDoc();
    }

    protected override T GetNextData<T>(HtmlDocument doc, string node) {
        var json = doc.QuerySelector("#__NEXT_DATA__").InnerText;
        return JsonDocument.Parse(json)
            .RootElement.GetProperty("props")
            .GetProperty("pageProps")
            .GetProperty(node)
            .GetRawText()
            .Deserialize<T>();
    }
}