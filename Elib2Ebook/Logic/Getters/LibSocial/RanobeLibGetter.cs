using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.SocialLib;
using Elib2Ebook.Types.RanobeLib;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters.LibSocial; 

public class RanobeLibGetter : LibSocialGetterBase {
    public RanobeLibGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ranobelib.me/");
    protected Uri ApiUrl => new("https://api.lib.social/api/manga/");
    protected Uri ImagesUrl => new("https://cover.imgslib.link/");
    protected override string GetId(Uri url) => url.GetSegment(3);

    public override Task Authorize() {
        return Task.CompletedTask;
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

    protected override Task<HtmlDocument> GetChapter(Uri url, SocialLibChapter chapter, User user) {
        return null;
    }

    private HttpRequestMessage GetDefaultMessage(Uri uri, Uri host, HttpContent content = null) {
        var message = new HttpRequestMessage(content == default ? HttpMethod.Get : HttpMethod.Post, uri);
        message.Content = content;
        message.Version = Config.Client.DefaultRequestVersion;
        
        foreach (var header in Config.Client.DefaultRequestHeaders) {
            message.Headers.Add(header.Key, header.Value);
        }

        message.Headers.Host = host.Host;

        return message;
    }

    private async Task<RanobeLibBookDetails> GetBookDetails(Uri url)
    {
        var book_url = ApiUrl.MakeRelativeUri(GetId(url));

        book_url = book_url.AppendQueryParameter("fields[]","background");
        book_url = book_url.AppendQueryParameter("fields[]","teams");
        book_url = book_url.AppendQueryParameter("fields[]","authors");
        book_url = book_url.AppendQueryParameter("fields[]","chap_count");

        var response = await Config.Client.SendWithTriesAsync(() => GetDefaultMessage(book_url, ApiUrl));
        if (response.StatusCode != HttpStatusCode.OK) {
            throw new Exception("Ошибка загрузки информации о книге");
        }

        return await response.Content.ReadFromJsonAsync<RanobeLibBookDetails>();
    }

    private async Task<RanobeLibBookChapters> GetBookChapters(RanobeLibBookDetails book)
    {
        var book_url = ApiUrl.MakeRelativeUri(book.Data.SlugUrl+"/chapters");

        Console.WriteLine($"Загружаю оглавление");

        var response = await Config.Client.SendWithTriesAsync(() => GetDefaultMessage(book_url, ApiUrl));
        if (response.StatusCode != HttpStatusCode.OK) {
            throw new Exception("Ошибка загрузки оглавления");
        }

        return await response.Content.ReadFromJsonAsync<RanobeLibBookChapters>();
    }

    private Author GetAuthor(RanobeLibBookDetails details) {
        var author = details.Data.Authors.ElementAt(0);
        return new Author(author.Name, SystemUrl.MakeRelativeUri($"/ru/people/{author.SlugUrl}"));
    }
    
    private IEnumerable<Author> GetCoAuthors(RanobeLibBookDetails details) {
        var result = new List<Author>();
        for(var i = 1; i < details.Data.Authors.Count; i ++) {
            var author = details.Data.Authors.ElementAt(i);
            result.Add( new Author(author.Name, SystemUrl.MakeRelativeUri($"/ru/people/{author.SlugUrl}")) );
        }

        return result;
    }
    private Task<Image> GetCover(RanobeLibBookDetails details) {
        return !string.IsNullOrWhiteSpace(details.Data.Cover.Default) ? SaveImage(details.Data.Cover.Default.AsUri()) : Task.FromResult(default(Image));
    }

    private async Task<IEnumerable<Chapter>> FillChapters(RanobeLibBookDetails book) {
        var chapters = new List<Chapter>();
        foreach (var rlbChapter in await GetChapters(book)) {
            var title = rlbChapter.Name.ReplaceNewLine();
            
            var chapter = new Chapter {
                Title = title
            };

            var chapterDoc = rlbChapter.Content.AsHtmlDoc();
            chapter.Images = await GetImages(chapterDoc, ImagesUrl);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;

            chapters.Add(chapter);
        }
            
        return chapters;
    }

    private async Task<List<RanobeLibBookChapter>> GetChapters(RanobeLibBookDetails book) {
        var _chapters = await GetBookChapters(book);
        var chapters = SliceToc(_chapters.Chapters);
        var result = new List<RanobeLibBookChapter>();
        
        foreach (var chapter in chapters) {
            var uri = ApiUrl.MakeRelativeUri($"{book.Data.SlugUrl}/chapter?number={chapter.Number}&volume={chapter.Volume}");
            Console.WriteLine($"Загружаю том {chapter.Volume} глава {chapter.Number}");
            var response = await Config.Client.SendWithTriesAsync(() => GetDefaultMessage(uri, ApiUrl));
            var _chapter = await response.Content.ReadFromJsonAsync<RanobeLibBookChapterResponse>();
            result.Add(_chapter.Data);
        }

        return result;
    }
}