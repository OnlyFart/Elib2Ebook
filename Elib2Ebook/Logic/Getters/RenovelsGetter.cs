using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Renovels;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class RenovelsGetter : GetterBase{
    public RenovelsGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://renovels.org/");

    protected override string GetId(Uri url) {
        return url.Segments[2].Trim('/');
    }

    public override async Task<Book> Get(Uri url) {
        url = new Uri($"https://renovels.org/novel/{GetId(url)}");
        var doc = await _config.Client.GetHtmlDocWithTriesAsync(url);

        var content = GetNextData<RenovelsContent>(doc, "content");

        var book = new Book(url) {
            Cover = await GetCover(content, url),
            Chapters = await FillChapters(content, url),
            Title = content.RusName,
            Author = new Author("Renovels"),
            Annotation = content.Description
        };
            
        return book;
    }
    
    private async Task<IEnumerable<Chapter>> FillChapters(RenovelsContent content, Uri url) {
        var result = new List<Chapter>();
            
        foreach (var ranobeChapter in await GetToc(content)) {
            Console.WriteLine($"Загружаю главу {ranobeChapter.Title.CoverQuotes()}");
            var chapter = new Chapter();
            var doc = await GetChapter(ranobeChapter);
            chapter.Images = await GetImages(doc, url);
            chapter.Content = doc.DocumentNode.InnerHtml;
            chapter.Title = ranobeChapter.Title;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(RenovelsChapter ranobeChapter) {
        var response = await _config.Client.GetFromJsonAsync<RenovelsChapterResponse<RenovelsChapter>>($"https://api.renovels.org/api/titles/chapters/{ranobeChapter.Id}/");
        return response.Content.Content.AsHtmlDoc();
    }

    private Task<Image> GetCover(RenovelsContent book, Uri bookUri) {
        var imagePath = book.Img.GetValueOrDefault("high", null) ?? book.Img.FirstOrDefault().Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? GetImage(new Uri(bookUri, imagePath)) : Task.FromResult(default(Image));
    }
    
    private static T GetNextData<T>(HtmlDocument doc, string node) {
        var json = doc.QuerySelector("#__NEXT_DATA__").InnerText;
        return JsonDocument.Parse(json)
            .RootElement.GetProperty("props")
            .GetProperty("pageProps")
            .GetProperty("fallbackData")
            .GetProperty(node)
            .GetRawText()
            .Deserialize<T>();
    }

    private async Task<IEnumerable<RenovelsChapter>> GetToc(RenovelsContent content) {
        var result = new List<RenovelsChapter>();
        
        for (var i = 1;; i++) {
            var uri = $"https://api.renovels.org/api/titles/chapters/?branch_id={content.Branches[0].Id}&ordering=index&user_data=1&count=40&page={i}";
            var response = await _config.Client.GetFromJsonAsync<RenovelsChapterResponse<RenovelsChapter[]>>(uri);
            result.AddRange(response!.Content);

            if (response.Content.Length < 40) {
                return result.Where(c => !c.IsPaid || c.IsBought == true);
            }
        }
    }
}