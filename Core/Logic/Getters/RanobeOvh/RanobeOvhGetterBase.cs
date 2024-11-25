using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using Core.Types.RanobeOvh;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters.RanobeOvh; 

public abstract class RanobeOvhGetterBase : GetterBase {
    protected RanobeOvhGetterBase(BookGetterConfig config) : base(config) { }

    protected override string GetId(Uri url) {
        return url.GetSegment(2);
    }

    protected abstract Task<HtmlDocument> GetChapter(RanobeOvhChapterShort ranobeOvhChapterFull);
    
    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri($"/content/{GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        
        var manga = GetNextData<RanobeOvhManga>(doc, "book");
        var chapters = GetNextData<RanobeOvhChapterShort[]>(doc, "chapters");
        var branch = GetBranch(doc);

        var book = new Book(url) {
            Cover = await GetCover(manga, url),
            Chapters = await FillChapters(branch, chapters, url),
            Title = manga.Name.Ru,
            Author = GetAuthor(branch),
            Annotation = manga.Description
        };

        return book;
    }
    
    protected override HttpRequestMessage GetImageRequestMessage(Uri uri) {
        var message = base.GetImageRequestMessage(uri);
        message.Headers.Referrer = SystemUrl;
        return message;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(RanobeOvhBranch branch, RanobeOvhChapterShort[] chapters, Uri url) {
        var result = new List<Chapter>();
        if (Config.Options.NoChapters) {
            return result;
        }

        foreach (var ranobeOvhChapter in GetToc(branch, chapters)) {
            var chapter = new Chapter();
            Config.Logger.LogInformation($"Загружаю главу {ranobeOvhChapter.FullName.CoverQuotes()}");

            var chapterDoc = await GetChapter(ranobeOvhChapter);
            chapter.Title = ranobeOvhChapter.FullName;
            chapter.Images = await GetImages(chapterDoc, url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;

            result.Add(chapter);
        }

        return result;
    }

    private T GetNextData<T>(HtmlDocument doc, string node) {
        var json = Regex.Match(doc.ParsedText, "__remixContext = (?<data>.*?);</script>", RegexOptions.Singleline).Groups["data"].Value;
        return JsonDocument.Parse(json)
            .RootElement.GetProperty("state")
            .GetProperty("loaderData")
            .GetProperty("routes/reader/book/$slug/index")
            .GetProperty(node)
            .GetRawText()
            .Deserialize<T>();
    }

    private IEnumerable<RanobeOvhChapterShort> GetToc(RanobeOvhBranch branch, RanobeOvhChapterShort[] chapters) {
        return SliceToc(chapters.Where(c => c.BranchId == branch.Id).Reverse().ToList(), c => c.FullName);
    }

    private RanobeOvhBranch GetBranch(HtmlDocument doc) {
        var branches = GetNextData<RanobeOvhBranch[]>(doc, "branches");
        return branches.MaxBy(c => c.ChaptersCount);
    }

    private Author GetAuthor(RanobeOvhBranch branch) {
        if (branch.Translators == null) {
            return new Author("RanobeOvh");
        }
        
        var translator = branch.Translators[0];
        return new Author(translator.Name, SystemUrl.MakeRelativeUri($"/translator/{translator.Slug}"));
    }

    private Task<TempFile> GetCover(RanobeOvhManga manga, Uri uri) {
        return !string.IsNullOrWhiteSpace(manga.Poster) ? SaveImage(uri.MakeRelativeUri(manga.Poster)) : Task.FromResult(default(TempFile));
    }
}