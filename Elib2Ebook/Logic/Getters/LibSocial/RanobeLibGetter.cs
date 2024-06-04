using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.RanobeLib;
using HtmlAgilityPack;

namespace Elib2Ebook.Logic.Getters.LibSocial; 

public class RanobeLibGetter : GetterBase {
    public RanobeLibGetter(BookGetterConfig config) : base(config) { }
    
    protected override Uri SystemUrl => new("https://ranobelib.me/");
    
    private static Uri ApiUrl => new("https://api.lib.social/api/manga/");
    
    private static Uri ImagesUrl => new("https://cover.imgslib.link/");
    
    protected override string GetId(Uri url) {
        var id = url.GetSegment(2);
        return id is "book" or "read" ? url.GetSegment(3) : id;
    }

    public override async Task<Book> Get(Uri url) {
        var details = await GetBookDetails(url);

        var book = new Book(url) {
            Cover = await GetCover(details),
            Chapters = await FillChapters(details),
            Title = details.Data.Name,
            Author = GetAuthor(details),
            CoAuthors = GetCoAuthors(details)
        };

        return book;
    }

    private async Task<RanobeLibBookDetails> GetBookDetails(Uri url) {
        url = ApiUrl.AppendSegment(GetId(url))
            .AppendQueryParameter("fields[]", "background")
            .AppendQueryParameter("fields[]", "teams")
            .AppendQueryParameter("fields[]", "authors")
            .AppendQueryParameter("fields[]", "chap_count");

        var response = await Config.Client.GetWithTriesAsync(url);
        if (response.StatusCode != HttpStatusCode.OK) {
            throw new Exception("Ошибка загрузки информации о книге");
        }

        return await response.Content.ReadFromJsonAsync<RanobeLibBookDetails>();
    }

    private async Task<IEnumerable<RanobeLibBookChapter>> GetToc(RanobeLibBookDetails book) {
        var url = ApiUrl.MakeRelativeUri(book.Data.SlugUrl).AppendSegment("chapters");

        Console.WriteLine("Загружаю оглавление");

        var response = await Config.Client.GetWithTriesAsync(url);
        if (response.StatusCode != HttpStatusCode.OK) {
            throw new Exception("Ошибка загрузки оглавления");
        }

        return SliceToc(await response.Content.ReadFromJsonAsync<RanobeLibBookChapters>().ContinueWith(t => t.Result.Chapters));
    }

    private Author GetAuthor(RanobeLibBookDetails details) {
        var author = details.Data.Authors.FirstOrDefault();
        return author == default ? new Author("Ranobelib") : new Author(author.Name, SystemUrl.MakeRelativeUri($"/ru/people/{author.SlugUrl}"));
    }
    
    private IEnumerable<Author> GetCoAuthors(RanobeLibBookDetails details) {
        return details.Data.Authors
            .Skip(1)
            .Select(author => new Author(author.Name, SystemUrl.MakeRelativeUri($"/ru/people/{author.SlugUrl}"))).ToList();
    }
    
    private Task<Image> GetCover(RanobeLibBookDetails details) {
        return !string.IsNullOrWhiteSpace(details.Data.Cover.Default) ? SaveImage(details.Data.Cover.Default.AsUri()) : Task.FromResult(default(Image));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(RanobeLibBookDetails book) {
        var chapters = new List<Chapter>();
        
        foreach (var rlbChapter in await GetToc(book)) {
            var title = rlbChapter.Name.ReplaceNewLine();
            Console.WriteLine($"Загружаю главу {title.CoverQuotes()}");
            
            var chapter = new Chapter {
                Title = title
            };

            var chapterDoc = await GetChapter(book, rlbChapter);
            chapter.Images = await GetImages(chapterDoc, ImagesUrl);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;

            chapters.Add(chapter);
        }
            
        return chapters;
    }

    private async Task<HtmlDocument> GetChapter(RanobeLibBookDetails book, RanobeLibBookChapter chapter) {
        var uri = ApiUrl.MakeRelativeUri($"{book.Data.SlugUrl}/chapter?number={chapter.Number}&volume={chapter.Volume}");
        var response = await Config.Client.GetWithTriesAsync(uri, TimeSpan.FromSeconds(10));
        var cc = await response.Content.ReadFromJsonAsync<RanobeLibBookChapterResponse>(); 
        return cc.Data.GetHtmlDoc();
    }
}