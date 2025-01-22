using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using Core.Types.Wattpad;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters; 

public class WattpadGetter(BookGetterConfig config) : GetterBase(config) {
    protected override Uri SystemUrl => new("https://wattpad.com/");

    protected override string GetId(Uri url) => base.GetId(url).Split('-')[0];

    private async Task<string> GetStoryId(Uri url) {
        if (url.ToString().Contains("/story/")) {
            return GetId(url);
        }

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        return doc.QuerySelector("button[data-story-id]").Attributes["data-story-id"].Value;
    }

    public override async Task<Book> Get(Uri url) {
        var storyId = await GetStoryId(url);
        url = SystemUrl.MakeRelativeUri(storyId);
        var wattpadInfo = await Config.Client.GetFromJsonAsync<WattpadInfo>(SystemUrl.MakeRelativeUri($"/api/v3/stories/{storyId}"));

        var book = new Book(url) {
            Cover = await GetCover(wattpadInfo),
            Chapters = await FillChapters(wattpadInfo),
            Title = wattpadInfo.Title,
            Author = new Author(wattpadInfo.User.Name, SystemUrl.MakeRelativeUri($"/user/{wattpadInfo.User.Name}")),
            Annotation = wattpadInfo.Description
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(WattpadInfo wattpadInfo) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }
            
        foreach (var group in SliceToc(wattpadInfo.Parts, c => c.FullName)) {
            Config.Logger.LogInformation($"Загружаю главу {group.FullName.CoverQuotes()}");
            var chapter = new Chapter();
                
            var chapterDoc = await GetChapter(group);
            chapter.Images = await GetImages(chapterDoc, group.Url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = group.FullName;

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(WattpadPart part) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(part.Url);
        foreach (var node in doc.QuerySelectorAll("p")) {
            node.Attributes.RemoveAll();
        }
        
        return doc;
    }

    private Task<TempFile> GetCover(WattpadInfo wattpadInfo) {
        return !string.IsNullOrWhiteSpace(wattpadInfo.Cover) ? SaveImage(wattpadInfo.Cover.AsUri()) : Task.FromResult(default(TempFile));
    }
}