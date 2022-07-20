using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.RanobeOvh;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class RanobeOvhGetter : GetterBase {
    public RanobeOvhGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ranobe.ovh/");
    
    private async Task<Uri> GetMainUrl(Uri url) {
        if (url.Segments[1] != "ranobe/") {
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
            var branch = GetNextData<RanobeOvhBranch>(doc, "branch");
            return new Uri($"https://ranobe.ovh/ranobe/{branch.Book.Slug}");
        }

        return url;
    }
    
    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(new Uri($"https://ranobe.ovh/ranobe/{GetId(url)}"));
        
        var manga = GetNextData<RanobeOvhManga>(doc, "manga");
        var branch = GetBranch(doc);

        var book = new Book(url) {
            Cover = await GetCover(manga, url),
            Chapters = await FillChapters(branch, url),
            Title = manga.Name.Ru,
            Author = GetAuthor(branch),
            Annotation = manga.Description.Ru
        };

        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(RanobeOvhBranch branch, Uri url) {
        var result = new List<Chapter>();

        foreach (var ranobeOvhChapter in await GetToc(branch)) {
            var chapter = new Chapter();
            Console.WriteLine($"Загружаю главу {ranobeOvhChapter.FullName.CoverQuotes()}");

            var chapterDoc = await GetChapter(ranobeOvhChapter);
            chapter.Title = ranobeOvhChapter.FullName;
            chapter.Images = await GetImages(chapterDoc, url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(RanobeOvhChapter ranobeOvhChapter) {
        var data = await Config.Client.GetFromJsonAsync<RanobeOvhChapter>($"https://api.ranobe.ovh/chapter/{ranobeOvhChapter.Id}");
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

    private async Task<IEnumerable<RanobeOvhChapter>> GetToc(RanobeOvhBranch branch) {
        var data = await Config.Client.GetStringAsync(new Uri($"https://api.ranobe.ovh/branch/{branch.Id}/chapters"));
        return data.Deserialize<RanobeOvhChapter[]>().Reverse();
    }

    private static RanobeOvhBranch GetBranch(HtmlDocument doc) {
        var branches = GetNextData<RanobeOvhBranch[]>(doc, "branches");
        return branches[0];
    }

    private static Author GetAuthor(RanobeOvhBranch branch) {
        var translator = branch.Translators[0];
        return new Author(translator.Name, new Uri($"https://ranobe.ovh/translator/{translator.Slug}"));
    }

    private Task<Image> GetCover(RanobeOvhManga manga, Uri uri) {
        return !string.IsNullOrWhiteSpace(manga.Poster) ? GetImage(new Uri(uri, manga.Poster)) : Task.FromResult(default(Image));
    }
    
    private static T GetNextData<T>(HtmlDocument doc, string node) {
        var json = doc.QuerySelector("#__NEXT_DATA__").InnerText;
        return JsonDocument.Parse(json)
            .RootElement.GetProperty("props")
            .GetProperty("pageProps")
            .GetProperty(node)
            .GetRawText()
            .Deserialize<T>();
    }
}