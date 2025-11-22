using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters;

public class BoovelGetter(BookGetterConfig config) : GetterBase(config) {
    
    protected override Uri SystemUrl => new("https://boovell.ru/");

    protected override string GetId(Uri url) {
        return url.GetSegment(2);
    }

    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri($"/story/{GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc),
            Title = doc.GetTextBySelector("h1.story__identity-title"),
            Author = new Author("Boovell"),
            Annotation = doc.QuerySelector("section.story__summary")?.InnerHtml
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }
            
        foreach (var urlChapter in GetToc(doc)) {
            Config.Logger.LogInformation($"Загружаю главу {urlChapter.Title.CoverQuotes()}");
            var chapter = new Chapter {
                Title = urlChapter.Title
            };

            var chapterDoc = await GetChapter(urlChapter);

            if (chapterDoc != default) {
                chapter.Images = await GetImages(chapterDoc, SystemUrl);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }
            
            result.Add(chapter);
        }

        return result;
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc) {
        var result = doc
            .QuerySelectorAll("a.chapter-group__list-item-link[href]")
            .Select(a => new UrlChapter(SystemUrl.MakeRelativeUri(a.Attributes["href"].Value), a.GetText().ReplaceNewLine()))
            .ToList();

        return SliceToc(result, c => c.Title);
    }

    private async Task<HtmlDocument> GetChapter(UrlChapter urlChapter) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url);
        var encoded = Convert.FromBase64String(doc.QuerySelector("#chapter-content").InnerText);
        var secret = doc.QuerySelector("body").Attributes["data-post-id"].Value + "skajldayzonePVPkruto0";

        var crypto = SHA256.HashData(Encoding.UTF8.GetBytes(secret).AsSpan(0, Encoding.UTF8.GetByteCount(secret)));
        using var aes = Aes.Create();
        
        const int IV_SHIFT = 16;

        aes.Key = crypto; 
        aes.IV = encoded[..IV_SHIFT];
        
        var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(encoded[IV_SHIFT..]);
        await using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        
        var output = new MemoryStream();
        await cs.CopyToAsync(output);

        return Encoding.UTF8.GetString(output.ToArray()).AsHtmlDoc();
    }

    private Task<TempFile> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("img.wp-post-image")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(TempFile));
    }
}