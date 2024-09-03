using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Boosty;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters;

public class BoostyGetter : GetterBase {
    public BoostyGetter(BookGetterConfig config) : base(config) { }
    
    protected override Uri SystemUrl => new("https://boosty.to/");

    private static Uri ApiUrl => new(" https://api.boosty.to/");

    protected override string GetId(Uri url) {
        return url.GetSegment(1);
    }

    public override async Task<Book> Get(Uri url) {
        var bookId = GetId(url);
        url = SystemUrl.MakeRelativeUri($"/{bookId}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(url, bookId),
            Title = "Exclusive content on Boosty",
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div.AboutAuthor_content_HprOc")?.InnerHtml,
        };
            
        return book;
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("h1");
        return new Author(a.GetText(), url);
    }

    private async Task<IEnumerable<Chapter>> FillChapters(Uri uri, string bookId) {
        var result = new List<Chapter>();
        
        foreach (var post in await GetToc(bookId)) {
            Config.Logger.LogInformation($"Загружаю главу {post.Title.CoverQuotes()}");
            var chapter = new Chapter {
                Title = post.Title
            };
            
            var doc = await GetChapter(post, bookId);
            if (doc != default) {
                chapter.Images = await GetImages(doc, uri);
                chapter.Content = doc.DocumentNode.InnerHtml;
            }
            
            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(BoostyPost post, string bookId) {
        if (!post.HasAccess) {
            return default;
        }
        
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri($"/{bookId}/posts/{post.Id}"));
        doc = doc.QuerySelector("article.Post_content_ETkyT").InnerHtml.AsHtmlDoc().RemoveNodes("h1");
        
        foreach (var img in doc.QuerySelectorAll("img")) {
            if (img.ParentNode.Name == "a") {
                img.ParentNode.Name = "div";
                img.ParentNode.Attributes.RemoveAll();
            }
        }

        foreach (var div in doc.QuerySelectorAll("div.BlockRenderer_markup_Wtipg")) {
            if (string.IsNullOrWhiteSpace(div.InnerHtml)) {
                div.InnerHtml = "<br />";
            }
        }
        
        return doc;
    }

    private async Task<IEnumerable<BoostyPost>> GetToc(string bookId) {
        var result = new List<BoostyPost>();
        
        var response = await Config.Client.GetWithTriesAsync(ApiUrl.MakeRelativeUri($"/v1/blog/{bookId}/post/").AppendQueryParameter("limit", 10));
        var content = await response.Content.ReadFromJsonAsync<BoostyApiResponse<BoostyPost[]>>();
        result.AddRange(content.Data);

        while (!content.Extra.IsLast) {
            response = await Config.Client.GetWithTriesAsync(ApiUrl.MakeRelativeUri($"/v1/blog/{bookId}/post/").AppendQueryParameter("limit", 10).AppendQueryParameter("offset", content.Extra.Offset));
            content = await response.Content.ReadFromJsonAsync<BoostyApiResponse<BoostyPost[]>>();
            result.AddRange(content.Data);
        }

        result.Reverse();
        return SliceToc(result);
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("link[rel=image_src]")?.Attributes["href"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}