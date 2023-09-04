using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.RanobeOvh;
using HtmlAgilityPack;

namespace Elib2Ebook.Logic.Getters.RanobeOvh; 

public abstract class RanobeOvhGetterBase : GetterBase {
    protected RanobeOvhGetterBase(BookGetterConfig config) : base(config) { }

    private Uri _apiUrl => new($"https://api.{SystemUrl.Host}/");
    
    protected abstract string Segment { get; }
    
    protected abstract Task<HtmlDocument> GetChapter(RanobeOvhChapter ranobeOvhChapter);
    
    protected abstract T GetNextData<T>(HtmlDocument doc, string node);

    private async Task<Uri> GetMainUrl(Uri url) {
        if (url.GetSegment(1) != Segment) {
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
            var branch = GetNextData<RanobeOvhBranch>(doc, "branch");
            return SystemUrl.MakeRelativeUri($"/{Segment}/{branch.Book.Slug}");
        }

        return url;
    }
    
    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri($"/{Segment}/{GetId(url)}"));
        
        var manga = GetNextData<RanobeOvhManga>(doc, "book");
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
    
    protected override HttpRequestMessage GetImageRequestMessage(Uri uri) {
        var message = base.GetImageRequestMessage(uri);
        message.Headers.Referrer = SystemUrl;
        return message;
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

    private async Task<IEnumerable<RanobeOvhChapter>> GetToc(RanobeOvhBranch branch) {
        var data = await Config.Client.GetStringAsync(_apiUrl.MakeRelativeUri($"/branch/{branch.Id}/chapters"));
        return SliceToc(data.Deserialize<RanobeOvhChapter[]>().Reverse().ToList());
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

    private Task<Image> GetCover(RanobeOvhManga manga, Uri uri) {
        return !string.IsNullOrWhiteSpace(manga.Poster) ? SaveImage(uri.MakeRelativeUri(manga.Poster)) : Task.FromResult(default(Image));
    }
}