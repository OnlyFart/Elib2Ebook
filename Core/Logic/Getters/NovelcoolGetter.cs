using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters;

public class NovelcoolGetter(BookGetterConfig config) : GetterBase(config) {
    protected override Uri SystemUrl => new("https://ru.novelcool.com/");
    protected Uri DataUrl => new("https://www.mastertheenglish.com/");

    protected override string GetId(Uri url) {
        return url.GetSegment(1);
    }

    public override async Task<Book> Get(Uri url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
 
        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1.bookinfo-title"),
            Author = new Author("NovelCool"),
            Annotation = GetAnnotation(doc),
            Lang = "ru"
        };
        
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        var toc = GetToc(doc, url);

        foreach (var urlChapter in toc) {
            Config.Logger.LogInformation($"Загружаю главу {urlChapter.Title.CoverQuotes()}");

            var chapter = new Chapter {
                Title = urlChapter.Title
            };
            
            var chapterDoc = await GetChapter(urlChapter.Url, url);
            chapter.Images = await GetImages(chapterDoc, SystemUrl);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri chapterUrl, Uri referrer) {
        // important Referrer, do not remove
        Config.Client.DefaultRequestHeaders.Remove("Referer");
        Config.Client.DefaultRequestHeaders.Add("Referer", referrer.ToString());
        var response = await Config.Client.GetWithTriesAsync(chapterUrl);

        var doc = await response.Content.ReadAsStreamAsync().ContinueWith(t => t.Result.AsHtmlDoc(null));
        var url = doc.QuerySelector("div.post-content-body a[href]").Attributes["href"].Value as string;
        return await GetChapterReal( url.AsUri(), response.RequestMessage.RequestUri );
    }

    private async Task<HtmlDocument> GetChapterReal(Uri chapterUrl, Uri referrer) {
        // important Referrer, do not remove
        Config.Client.DefaultRequestHeaders.Remove("Referer");
        Config.Client.DefaultRequestHeaders.Add("Referer", referrer.ToString());
        var redirection_response = await Config.Client.GetWithTriesAsync(chapterUrl);

        var redirection_doc = await redirection_response.Content.ReadAsStreamAsync().ContinueWith(t => t.Result.AsHtmlDoc(null));

        var redirection_path = Regex.Match(redirection_doc.ParsedText, @"window\.location\.href=""(?<data>.*)"";", RegexOptions.Multiline | RegexOptions.Singleline).Groups["data"].Value;
        var redirection_url = DataUrl.MakeRelativeUri(redirection_path);

        // important Referrer, do not remove
        Config.Client.DefaultRequestHeaders.Remove("Referer");
        Config.Client.DefaultRequestHeaders.Add("Referer", redirection_response.RequestMessage.RequestUri.ToString());
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(redirection_url);

        var temp = Regex.Match(doc.ParsedText, @"all_imgs_url\:\s+(?<data>\[(.*)\]),", RegexOptions.Multiline | RegexOptions.Singleline).Groups["data"].Value;
        temp = Regex.Replace(temp, @",\s+\]", "]");
        var json = temp.Deserialize<JsonArray>();
    
        var sb = new StringBuilder();

        foreach (var elem in json) {
            switch (elem) {
                case JsonArray images: {
                    foreach (var image in images) {
                        sb.Append($"<img src='{image.ToString()}'/>");
                    }

                    break;
                }
                default:
                    sb.Append($"<img src='{elem.ToString()}'/>");
                    break;
            }
        }

        return sb.AsHtmlDoc();
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        var chapters = doc.QuerySelectorAll("div.chapter-item-list div.chp-item a[href]").Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.Attributes["title"].Value)).ToList();
        return SliceToc(chapters, c => c.Title);
    }

    private static string GetAnnotation(HtmlDocument doc) {
        return doc.QuerySelector("div.bk-summary div.bk-summary-txt")?.InnerHtml;
    }

    private Task<TempFile> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("img.bookinfo-pic-img")?.Attributes["src"]?.Value;
        if( imagePath != "/files/images/default/default_pic.jpg" && !string.IsNullOrWhiteSpace(imagePath) )
        {
            return SaveImage(uri.MakeRelativeUri(imagePath));
        }
        return Task.FromResult(default(TempFile));
    }
    
    private static Author GetAuthor(HtmlDocument doc) {
        var a = doc.QuerySelector("meta[property='books:author']");
        return a != default ? new Author(a.Attributes["content"].Value) : new Author("NovelHall");
    }
}