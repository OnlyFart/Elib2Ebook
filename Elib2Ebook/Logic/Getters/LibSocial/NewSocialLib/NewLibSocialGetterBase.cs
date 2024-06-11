using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.SocialLib;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters.LibSocial.NewSocialLib;

public abstract class NewLibSocialGetterBase : GetterBase{
    protected NewLibSocialGetterBase(BookGetterConfig config) : base(config) { }
    
    private static Uri AuthHost => new("https://auth.lib.social/");

    private static Uri ApiHost => new("https://api.lib.social/");

    private static Uri BaseImageHost => new("https://cover.imgslib.link/");
    
    protected virtual Uri ImagesHost => BaseImageHost;

    private const string ALPHABET_BASE = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const string ALPHABET_CHALLENGE = ALPHABET_BASE + "-_";
    
    private static string Challenge(string str) {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(str).AsSpan(0, Encoding.UTF8.GetByteCount(str)));
        var result = string.Empty;
        
        for (var t = 0; t < bytes.Length; t += 3) {
            result += ALPHABET_CHALLENGE[bytes[t] >> 2];
            result += ALPHABET_CHALLENGE[(bytes[t] & 3) << 4 | bytes[t + 1] >> 4];
            result += ALPHABET_CHALLENGE[(bytes[t + 1] & 15) << 2 | (t + 2 < bytes.Length ? bytes[t + 2] : 0) >> 6];
            result += ALPHABET_CHALLENGE[(t + 2 < bytes.Length ? bytes[t + 2] : 0) & 63];
        }

        return (bytes.Length % 3) switch {
            2 => result[..^1],
            1 => result[..^2],
            _ => result
        };
    }

    private static string GetRandom(int length) {
        return Enumerable
            .Repeat(0, length)
            .Aggregate(string.Empty, (c, _) => c + ALPHABET_BASE[new Random().Next(ALPHABET_BASE.Length)]);
    }

    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }

        var secret = GetRandom(128);
        var state = GetRandom(40);
        var redirectUri = SystemUrl.MakeRelativeUri("/ru/front/auth/oauth/callback");
        
        var challenge = Challenge(secret);

        await Config.Client.GetAsync(AuthHost.MakeRelativeUri($"/auth/oauth/authorize?client_id=1&code_challenge={challenge}&code_challenge_method=S256&prompt=consent&redirect_uri={redirectUri}&response_type=code&scope=&state={state}"));
        var loginForm = await Config.Client.GetHtmlDocWithTriesAsync(AuthHost.MakeRelativeUri("/auth/login-form"));

        var payload = new Dictionary<string, string> {
            { "_token", loginForm.QuerySelector("input[name=_token]").Attributes["value"].Value },
            { "login", Config.Options.Login },
            { "password", Config.Options.Password },
        };
        
        var login = await Config.Client.PostHtmlDocWithTriesAsync(AuthHost.MakeRelativeUri("/auth/login"), new FormUrlEncodedContent(payload));
        var error = login.QuerySelector(".form-field__error");
        if (error != default && !string.IsNullOrWhiteSpace(error.InnerText)) {
            throw new Exception($"Не удалось авторизоваться. {error.InnerText}");
        }

        var postForm = login.QuerySelector("form[method=post]");
        payload = postForm
            .QuerySelectorAll("input[type=hidden]")
            .Select(input => input.Attributes["name"].Value)
            .ToDictionary(name => name, name => login.QuerySelector($"input[name={name}]").Attributes["value"].Value);
        
        var authorize = await Config.Client.PostAsync(AuthHost.MakeRelativeUri(postForm.Attributes["action"].Value), new FormUrlEncodedContent(payload));
        var tokenResponse = await Config.Client.PostAsync(ApiHost.MakeRelativeUri("/api/auth/oauth/token"), JsonContent.Create(new {
            grant_type = "authorization_code",
            client_id = 1,
            redirect_uri = redirectUri,
            code_verifier = secret,
            code = authorize.RequestMessage?.RequestUri.GetQueryParameter("code")
        }));

        var token = await tokenResponse.Content.ReadFromJsonAsync<LibSocialToken>();

        Config.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        Console.WriteLine("Успешно авторизовались");
    }

    protected override string GetId(Uri url) {
        var id = url.GetSegment(2);
        return id is "book" or "read" or "manga" ? url.GetSegment(3) : id;
    }

    public override async Task<Book> Get(Uri url) {
        var bid = url.GetQueryParameter("bid");
        var details = await GetBookDetails(url);

        var book = new Book(url) {
            Cover = await GetCover(details),
            Chapters = await FillChapters(details, bid),
            Title = string.IsNullOrWhiteSpace(details.Data.RusName) ? details.Data.Name : details.Data.RusName,
            Author = GetAuthor(details),
            CoAuthors = GetCoAuthors(details)
        };

        return book;
    }

    private async Task<RanobeLibBookDetails> GetBookDetails(Uri url) {
        url = ApiHost
            .AppendSegment("api/manga/" + GetId(url))
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

    private async Task<IEnumerable<SocialLibBookChapter>> GetToc(RanobeLibBookDetails book, string bid) {
        var url = ApiHost.MakeRelativeUri($"/api/manga/{book.Data.SlugUrl}/chapters");

        Console.WriteLine("Загружаю оглавление");

        var response = await Config.Client.GetWithTriesAsync(url);
        if (response.StatusCode != HttpStatusCode.OK) {
            throw new Exception("Ошибка загрузки оглавления");
        }

        var result = await response.Content.ReadFromJsonAsync<SocialLibBookChapters>().ContinueWith(t => t.Result.Chapters);
        if (!string.IsNullOrWhiteSpace(bid)) {
            result = result.Where(c => c.Branches.Any(b => b.BranchId.ToString() == bid)).ToList();
        }
        
        return SliceToc(result);
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

    protected override HttpRequestMessage GetImageRequestMessage(Uri uri) {
        uri = uri.Host == BaseImageHost.Host ? uri : uri.ReplaceHost(ImagesHost.Host);
        
        var message = base.GetImageRequestMessage(uri);
        message.Headers.Add("Referer", SystemUrl.ToString());
        return message;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(RanobeLibBookDetails book, string bid) {
        var chapters = new List<Chapter>();
        
        foreach (var socialChapter in await GetToc(book, bid)) {
            var title = socialChapter.Name.ReplaceNewLine();
            Console.WriteLine($"Загружаю главу {title.CoverQuotes()}");
            
            var chapter = new Chapter {
                Title = title
            };

            var chapterDoc = await GetChapter(book, socialChapter, bid);
            chapter.Images = await GetImages(chapterDoc, ImagesHost);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;

            chapters.Add(chapter);
        }
            
        return chapters;
    }

    private async Task<HtmlDocument> GetChapter(RanobeLibBookDetails book, SocialLibBookChapter chapter, string bid) {
        var uri = ApiHost.MakeRelativeUri($"/api/manga/{book.Data.SlugUrl}/chapter?number={chapter.Number}&volume={chapter.Volume}");
        if (!string.IsNullOrWhiteSpace(bid)) {
            uri = uri.AppendQueryParameter("branch_id", bid);
        }
        
        var chapterResponse = await Config.Client.GetFromJsonWithTriesAsync<SocialLibBookChapterResponse>(uri, TimeSpan.FromSeconds(30));
        return ResponseToHtmlDoc(chapterResponse.Data);
    }

    protected abstract HtmlDocument ResponseToHtmlDoc(SocialLibBookChapter chapterResponse);
}